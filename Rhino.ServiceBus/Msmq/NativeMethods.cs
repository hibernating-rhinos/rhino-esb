using System;
using System.Runtime.InteropServices;
using System.Transactions;

namespace Rhino.ServiceBus.Msmq
{
    public static class NativeMethods
    {
        public const int MQ_MOVE_ACCESS = 4;
        public const int MQ_DENY_NONE = 0;

        [DllImport("mqrt.dll", CharSet = CharSet.Unicode)]
        public static extern int MQOpenQueue(string formatName, int access, int shareMode, ref IntPtr hQueue);

        [DllImport("mqrt.dll")]
        public static extern int MQCloseQueue(IntPtr queue);

        [DllImport("mqrt.dll")]
        public static extern int MQMoveMessage(IntPtr sourceQueue, IntPtr targetQueue, long lookupID, IDtcTransaction transaction);

        [DllImport("mqrt.dll")]
        internal static extern int MQMgmtGetInfo([MarshalAs(UnmanagedType.BStr)]string computerName, [MarshalAs(UnmanagedType.BStr)]string objectName, ref MQMGMTPROPS mgmtProps);

        public const byte VT_NULL = 1;
        public const byte VT_UI4 = 19;
        public const int PROPID_MGMT_QUEUE_MESSAGE_COUNT = 7;

        //size must be 16
        [StructLayout(LayoutKind.Sequential)]
        internal struct MQPROPVariant
        {
            public byte vt;       //0
            public byte spacer;   //1
            public short spacer2; //2
            public int spacer3;   //4
            public uint ulVal;    //8
            public int spacer4;   //12
        }

        //size must be 16 in x86 and 28 in x64
        [StructLayout(LayoutKind.Sequential)]
        internal struct MQMGMTPROPS
        {
            public uint cProp;
            public IntPtr aPropID;
            public IntPtr aPropVar;
            public IntPtr status;

        }
    }
}
