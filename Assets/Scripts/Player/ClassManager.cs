using UnityEngine;
using System;

public class ClassManager : MonoBehaviour
{
    [System.Serializable]
    public class ClassInfo
    {
        public Class classType;
        public string className;
        public Sprite classIcon;
        public Gadget gadget;
    }


    public enum Class
    {
        Medic,
        Engineer,
        Assault,
        Support,
        Recoon,
        Squad_leader,
        Pilot
    }

    [SerializeField] private ClassInfo[] classes;

    public Sprite GetClassIcon(Class classType)
    {
        foreach (var classInfo in classes)
        {
            if (classInfo.classType == classType)
            {
                return classInfo.classIcon;
            }
        }
        return null;
    }

    public string GetClassName(Class classType)
    {
        foreach (var classInfo in classes)
        {
            if (classInfo.classType == classType)
            {
                return classInfo.className;
            }
        }
        return null;
    }

    public Gadget GetClassGadget(Class classType)
    {
        foreach (var classInfo in classes)
        {

            if (classInfo.classType == classType)
            {
                return classInfo.gadget;
            }
        }
        return null;
    }

}