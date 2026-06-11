#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class Quest3BuildConfigurator
{
    private const string MainScenePath = "Assets/Scenes/FruitNinja.unity";
    private const string OpenXRLoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";
    private const string OculusTouchFeatureId = "com.unity.openxr.feature.input.oculustouch";

    [MenuItem("Fruit Ninja/Configure Quest 3 Build")]
    public static void Configure()
    {
        EnsureMainSceneInBuildSettings();
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;

        bool loaderAssigned = TryAssignOpenXRLoader() || HasAndroidOpenXRLoaderAssigned();
        bool loaderAutoStarts = ForceAndroidOpenXRLoaderAutoStart();
        bool touchEnabled = TryEnableOpenXRFeature(OculusTouchFeatureId) || ForceEnableOpenXRFeature("OculusTouchControllerProfile Android");
        bool questEnabled = ForceEnableOpenXRFeature("OculusQuestFeature Android");

        AssetDatabase.SaveAssets();
        Debug.Log(
            "Quest3BuildConfigurator: Android Quest 3 basics configured. " +
            $"OpenXR loader assigned: {loaderAssigned}. Android XR auto-start: {loaderAutoStarts}. " +
            $"Quest support enabled: {questEnabled}. Oculus Touch profile enabled: {touchEnabled}. " +
            "If any value is false, open Project Settings > XR Plug-in Management > Android and enable OpenXR, Oculus Quest Support, and Oculus Touch Controller Profile manually.");
    }

    private static void EnsureMainSceneInBuildSettings()
    {
        if (!System.IO.File.Exists(MainScenePath))
        {
            Debug.LogError($"Fruit Ninja scene not found at {MainScenePath}.");
            return;
        }

        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Any(scene => scene.path == MainScenePath))
            return;

        EditorBuildSettings.scenes = scenes
            .Concat(new[] { new EditorBuildSettingsScene(MainScenePath, true) })
            .ToArray();
    }

    private static bool TryAssignOpenXRLoader()
    {
        Type storeType = FindType("UnityEditor.XR.Management.Metadata.XRPackageMetadataStore");
        if (storeType == null)
            return false;

        foreach (MethodInfo assignLoader in storeType.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (assignLoader.Name != "AssignLoader")
                continue;

            ParameterInfo[] parameters = assignLoader.GetParameters();
            if (parameters.Length != 3)
                continue;

            try
            {
                object result;
                if (parameters[0].ParameterType == typeof(string) && parameters[1].ParameterType == typeof(BuildTargetGroup))
                {
                    result = assignLoader.Invoke(null, new object[] { OpenXRLoaderTypeName, BuildTargetGroup.Android, null });
                    return result is bool success && success;
                }

                if (parameters[1].ParameterType == typeof(string) && parameters[2].ParameterType == typeof(BuildTargetGroup))
                {
                    object managerSettings = TryGetXRManagerSettings(BuildTargetGroup.Android);
                    if (managerSettings == null)
                        continue;

                    result = assignLoader.Invoke(null, new object[] { managerSettings, OpenXRLoaderTypeName, BuildTargetGroup.Android });
                    return result is bool success && success;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Quest3BuildConfigurator: failed to assign OpenXR loader: {exception.Message}");
                return false;
            }
        }

        return false;
    }

    private static bool TryEnableOpenXRFeature(string featureId)
    {
        Type settingsType = FindType("UnityEngine.XR.OpenXR.OpenXRSettings");
        if (settingsType == null)
            return false;

        MethodInfo getSettings = settingsType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method =>
                method.Name == "GetSettingsForBuildTargetGroup" &&
                method.GetParameters().Length == 1 &&
                method.GetParameters()[0].ParameterType == typeof(BuildTargetGroup));

        if (getSettings == null)
            return false;

        try
        {
            object settings = getSettings.Invoke(null, new object[] { BuildTargetGroup.Android });
            if (settings == null)
                return false;

            MethodInfo getFeatures = settingsType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(method => method.Name == "GetFeatures" && method.GetParameters().Length == 0);

            System.Collections.IEnumerable features = getFeatures?.Invoke(settings, Array.Empty<object>()) as System.Collections.IEnumerable;
            if (features == null)
                return false;

            foreach (object feature in features)
            {
                if (feature == null)
                    continue;

                Type featureType = feature.GetType();
                string id = ReadStringMember(feature, featureType, "featureIdInternal");
                if (id != featureId)
                    continue;

                if (!WriteBoolMember(feature, featureType, "m_enabled", true))
                    return false;

                EditorUtility.SetDirty((UnityEngine.Object)feature);
                EditorUtility.SetDirty((UnityEngine.Object)settings);
                return true;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Quest3BuildConfigurator: failed to enable OpenXR feature {featureId}: {exception.Message}");
        }

        return false;
    }

    private static bool ForceAndroidOpenXRLoaderAutoStart()
    {
        UnityEngine.Object androidProviders = FindXRSettingsAsset("Assets/XR/XRGeneralSettingsPerBuildTarget.asset", "Android Providers");
        if (androidProviders == null)
            return false;

        SerializedObject serializedProviders = new SerializedObject(androidProviders);
        bool changed = WriteSerializedBool(serializedProviders, "m_AutomaticLoading", true);
        changed |= WriteSerializedBool(serializedProviders, "m_AutomaticRunning", true);
        serializedProviders.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(androidProviders);
        return changed;
    }

    private static bool HasAndroidOpenXRLoaderAssigned()
    {
        UnityEngine.Object androidProviders = FindXRSettingsAsset("Assets/XR/XRGeneralSettingsPerBuildTarget.asset", "Android Providers");
        if (androidProviders == null)
            return false;

        SerializedObject serializedProviders = new SerializedObject(androidProviders);
        SerializedProperty loaders = serializedProviders.FindProperty("m_Loaders");
        if (loaders == null || !loaders.isArray)
            return false;

        for (int i = 0; i < loaders.arraySize; i++)
        {
            UnityEngine.Object loader = loaders.GetArrayElementAtIndex(i).objectReferenceValue;
            if (loader != null && loader.GetType().FullName == OpenXRLoaderTypeName)
                return true;
        }

        return false;
    }

    private static bool ForceEnableOpenXRFeature(string featureAssetName)
    {
        UnityEngine.Object feature = FindXRSettingsAsset("Assets/XR/Settings/OpenXR Package Settings.asset", featureAssetName);
        if (feature == null)
            return false;

        SerializedObject serializedFeature = new SerializedObject(feature);
        bool changed = WriteSerializedBool(serializedFeature, "m_enabled", true);
        serializedFeature.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(feature);
        return changed;
    }

    private static UnityEngine.Object FindXRSettingsAsset(string path, string assetName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .FirstOrDefault(asset => asset != null && asset.name == assetName);
    }

    private static bool WriteSerializedBool(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.propertyType != SerializedPropertyType.Boolean)
            return false;

        property.boolValue = value;
        return property.boolValue == value;
    }

    private static string ReadStringMember(object target, Type type, string name)
    {
        FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
            return field.GetValue(target) as string;

        PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return property?.GetValue(target) as string;
    }

    private static bool WriteBoolMember(object target, Type type, string name, bool value)
    {
        FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
            return true;
        }

        PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property != null && property.CanWrite)
        {
            property.SetValue(target, value);
            return true;
        }

        return false;
    }

    private static Type FindType(string fullName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(fullName);
            if (type != null)
                return type;
        }

        return null;
    }

    private static object TryGetXRManagerSettings(BuildTargetGroup buildTargetGroup)
    {
        Type perTargetType = FindType("UnityEngine.XR.Management.XRGeneralSettingsPerBuildTarget");
        if (perTargetType == null)
            return null;

        MethodInfo getSettings = perTargetType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method =>
                method.Name == "XRGeneralSettingsForBuildTarget" &&
                method.GetParameters().Length == 1 &&
                method.GetParameters()[0].ParameterType == typeof(BuildTargetGroup));

        object generalSettings = getSettings?.Invoke(null, new object[] { buildTargetGroup });
        if (generalSettings == null)
            return null;

        PropertyInfo managerProperty = generalSettings.GetType().GetProperty("Manager", BindingFlags.Public | BindingFlags.Instance);
        object manager = managerProperty?.GetValue(generalSettings);
        if (manager != null)
            return manager;

        FieldInfo managerField = generalSettings.GetType().GetField("m_LoaderManagerInstance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return managerField?.GetValue(generalSettings);
    }
}
#endif
