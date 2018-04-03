using System;
using System.Collections.Generic;

namespace NWN
{
    class Internal
    {
        public const uint OBJECT_INVALID = 0xFFFFFFFF;

        public static NWN.Object OBJECT_SELF { get; private set; }

        private static Stack<NWN.Object> s_ObjSelfStack = new Stack<NWN.Object>();

        void PushScriptContext(uint objId)
        {
            s_ObjSelfStack.Push(new NWN.Object { m_ObjId = objId });
            OBJECT_SELF = s_ObjSelfStack.Peek();
        }

        void PopScriptContext(uint objId)
        {
            s_ObjSelfStack.Pop();
            OBJECT_SELF = s_ObjSelfStack.Count == 0 ? null : s_ObjSelfStack.Peek();
        }

        public static void CallBuiltIn(int id)
        { }

        public static void StackPushInteger(int value)
        { }

        public static void StackPushFloat(float value)
        { }

        public static void StackPushString(string value)
        { }

        public static void StackPushObject(NWN.Object value, bool defAsObjSelf)
        {
            if (value == null)
            {
                value = defAsObjSelf ? OBJECT_SELF : OBJECT_INVALID;
            }
        }

        public static void StackPushVector(NWN.Vector? value)
        {
            if (!value.HasValue)
            {
                value = new NWN.Vector(0.0f, 0.0f, 0.0f);
            }
        }

        public static void StackPushEffect(NWN.Effect value)
        { }

        public static void StackPushEvent(NWN.Event value)
        { }

        public static void StackPushLocation(NWN.Location value)
        { }

        public static void StackPushTalent(NWN.Talent value)
        { }

        public static void StackPushItemProperty(NWN.ItemProperty value)
        { }

        public static int StackPopInteger()
        {
            return -1;
        }

        public static float StackPopFloat()
        {
            return -1.0f;
        }

        public static string StackPopString()
        {
            return "";
        }

        public static NWN.Object StackPopObject()
        {
            return OBJECT_INVALID;
        }

        public static NWN.Vector StackPopVector()
        {
            return new NWN.Vector(0.0f, 0.0f, 0.0f);
        }

        public static NWN.Effect StackPopEffect()
        {
            return new NWN.Effect { m_Handle = 0 };
        }

        public static NWN.Event StackPopEvent()
        {
            return new NWN.Event { m_Handle = 0 };
        }

        public static NWN.Location StackPopLocation()
        {
            return new NWN.Location { m_Handle = 0 };
        }

        public static NWN.Talent StackPopTalent()
        {
            return new NWN.Talent { m_Handle = 0 };
        }

        public static NWN.ItemProperty StackPopItemProperty()
        {
            return new NWN.ItemProperty { m_Handle = 0 };
        }
    }
}
