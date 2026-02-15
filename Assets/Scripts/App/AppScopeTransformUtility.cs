using UnityEngine;

public static class AppScopeTransformUtility
{
    public static void ReparentRootToAppScope(Component target, Transform appRoot, string owner, string fieldName)
    {
        if (target == null)
            return;

        ReparentRootToAppScope(target.transform, appRoot, owner, fieldName);
    }

    public static void ReparentRootToAppScope(GameObject target, Transform appRoot, string owner, string fieldName)
    {
        if (target == null)
            return;

        ReparentRootToAppScope(target.transform, appRoot, owner, fieldName);
    }

    public static void ReparentRootToAppScope(Transform target, Transform appRoot, string owner, string fieldName)
    {
        if (target == null || appRoot == null)
            return;

        Transform root = target.root;
        if (root == null || root == appRoot)
            return;

        root.SetParent(appRoot, true);
        Debug.Log($"[{owner}] Reparented {fieldName} root '{root.name}' to app scope.");
    }
}
