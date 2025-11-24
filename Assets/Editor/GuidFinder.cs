#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class GuidFinder
{
    [MenuItem("Tools/Find Asset By Guid")]
    private static void Find()
    {
        string guid = "a552161ad4ddba2abb00d9beba897af0";
        string path = AssetDatabase.GUIDToAssetPath(guid);
        Debug.Log($"GUID path = {path}");
        var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
        Selection.activeObject = obj;
    }
}
#endif