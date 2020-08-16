using System;
using static NativeMethods;
using System.Text;

public static class MemoryHelper
{
    public static int processHandle;
    public static bool readSuccess;
    public static bool writeSuccess;

    public static void Initialise()
    {
        processHandle = OpenProcess(0x38, false, GetCurrentProcessId());
    }

    public static byte ReadByte(int address)
    {
        return ReadBytes(address, 1)[0];
    }
    public static byte ReadByte(uint address)
    {
        return ReadBytes(address, 1)[0];
    }
    public static byte ReadByte(IntPtr address)
    {
        return ReadBytes(address, 1)[0];
    }
    public static byte ReadByte(UIntPtr address)
    {
        return ReadBytes(address, 1)[0];
    }


    public static int ReadInt(int address)
    {
        return BitConverter.ToInt32(ReadBytes(address, 4), 0);
    }
    public static int ReadInt(uint address)
    {
        return BitConverter.ToInt32(ReadBytes(address, 4), 0);
    }
    public static int ReadInt(IntPtr address)
    {
        return BitConverter.ToInt32(ReadBytes(address, 4), 0);
    }
    public static int ReadInt(UIntPtr address)
    {
        return BitConverter.ToInt32(ReadBytes(address, 4), 0);
    }


    public static uint ReadUInt(int address)
    {
        return BitConverter.ToUInt32(ReadBytes(address, 4), 0);
    }
    public static uint ReadUInt(uint address)
    {
        return BitConverter.ToUInt32(ReadBytes(address, 4), 0);
    }
    public static uint ReadUInt(IntPtr address)
    {
        return BitConverter.ToUInt32(ReadBytes(address, 4), 0);
    }
    public static uint ReadUInt(UIntPtr address)
    {
        return BitConverter.ToUInt32(ReadBytes(address, 4), 0);
    }


    public static UIntPtr ReadUIntPtr(int address)
    {
        return (UIntPtr)ReadUInt(address);
    }
    public static UIntPtr ReadUIntPtr(uint address)
    {
        return (UIntPtr)ReadUInt(address);
    }
    public static UIntPtr ReadUIntPtr(IntPtr address)
    {
        return (UIntPtr)ReadUInt(address);
    }
    public static UIntPtr ReadUIntPtr(UIntPtr address)
    {
        return (UIntPtr)ReadUInt(address);
    }


    public static IntPtr ReadIntPtr(int address)
    {
        return (IntPtr)ReadInt(address);
    }
    public static IntPtr ReadIntPtr(uint address)
    {
        return (IntPtr)ReadInt(address);
    }
    public static IntPtr ReadIntPtr(IntPtr address)
    {
        return (IntPtr)ReadInt(address);
    }
    public static IntPtr ReadIntPtr(UIntPtr address)
    {
        return (IntPtr)ReadInt(address);
    }

    public static float ReadFloat(int address)
    {
        return BitConverter.ToSingle(ReadBytes(address, 4), 0);
    }
    public static float ReadFloat(uint address)
    {
        return BitConverter.ToSingle(ReadBytes(address, 4), 0);
    }
    public static float ReadFloat(IntPtr address)
    {
        return BitConverter.ToSingle(ReadBytes(address, 4), 0);
    }
    public static float ReadFloat(UIntPtr address)
    {
        return BitConverter.ToSingle(ReadBytes(address, 4), 0);
    }


    public static bool ReadBoolean(int address)
    {
        return BitConverter.ToBoolean(ReadBytes(address, 4), 0);
    }
    public static bool ReadBoolean(uint address)
    {
        return BitConverter.ToBoolean(ReadBytes(address, 4), 0);
    }
    public static bool ReadBoolean(IntPtr address)
    {
        return BitConverter.ToBoolean(ReadBytes(address, 4), 0);
    }
    public static bool ReadBoolean(UIntPtr address)
    {
        return BitConverter.ToBoolean(ReadBytes(address, 4), 0);
    }


    public static byte[] ReadBytes(int address, int length)
    {
        return ReadBytes((uint)address, length);
    }
    public static byte[] ReadBytes(uint address, int length)
    {
        return ReadBytes((UIntPtr)address, length);
    }
    public static byte[] ReadBytes(IntPtr address, int length)
    {
        return ReadBytes((uint)address, length);
    }
    public static byte[] ReadBytes(UIntPtr address, int length)
    {
        byte[] bytes = new byte[length];
        readSuccess = ReadProcessMemory(processHandle, address, bytes, length, UIntPtr.Zero);
        return bytes;
    }


    public static string ReadString(int address)
    {
        return ReadString((UIntPtr)address);
    }
    public static string ReadString(uint address)
    {
        return ReadString((UIntPtr)address);
    }
    public static string ReadString(IntPtr address)
    {
        return ReadString((UIntPtr)(int)address);
    }
    public static string ReadString(UIntPtr address)
    {
        string s = "";
        while(true)
        {
            byte b = ReadByte(address += 1);
            if (b != 0)
            {
                s += (char)b;
            }
            else
            {
                return s;
            }
        }
    }


    public static void Write(int address, int value)
    {
        Write(address, BitConverter.GetBytes(value));
    }
    public static void Write(uint address, int value)
    {
        Write(address, BitConverter.GetBytes(value));
    }
    public static void Write(IntPtr address, int value)
    {
        Write(address, BitConverter.GetBytes(value));
    }
    public static void Write(UIntPtr address, int value)
    {
        Write(address, BitConverter.GetBytes(value));
    }

    public static void Write(int address, uint value)
    {
        Write(address, BitConverter.GetBytes(value));
    }
    public static void Write(uint address, uint value)
    {
        Write(address, BitConverter.GetBytes(value));
    }
    public static void Write(IntPtr address, uint value)
    {
        Write(address, BitConverter.GetBytes(value));
    }
    public static void Write(UIntPtr address, uint value)
    {
        Write(address, BitConverter.GetBytes(value));
    }


    public static void Write(int address, IntPtr value)
    {
        Write(address, BitConverter.GetBytes((int)value));
    }
    public static void Write(uint address, IntPtr value)
    {
        Write(address, BitConverter.GetBytes((int)value));
    }
    public static void Write(IntPtr address, IntPtr value)
    {
        Write(address, BitConverter.GetBytes((int)value));
    }
    public static void Write(UIntPtr address, IntPtr value)
    {
        Write(address, BitConverter.GetBytes((int)value));
    }


    public static void Write(int address, UIntPtr value)
    {
        Write(address, BitConverter.GetBytes((uint)value));
    }
    public static void Write(uint address, UIntPtr value)
    {
        Write(address, BitConverter.GetBytes((uint)value));
    }
    public static void Write(IntPtr address, UIntPtr value)
    {
        Write(address, BitConverter.GetBytes((uint)value));
    }
    public static void Write(UIntPtr address, UIntPtr value)
    {
        Write(address, BitConverter.GetBytes((uint)value));
    }


    public static void Write(int address, float number)
    {
        Write(address, BitConverter.GetBytes(number));
    }
    public static void Write(uint address, float value)
    {
        Write(address, BitConverter.GetBytes(value));
    }
    public static void Write(IntPtr address, float value)
    {
        Write(address, BitConverter.GetBytes(value));
    }
    public static void Write(UIntPtr address, float value)
    {
        Write(address, BitConverter.GetBytes(value));
    }


    public static void Write(int address, bool value)
    {
        Write(address, BitConverter.GetBytes(value));
    }
    public static void Write(uint address, bool value)
    {
        Write(address, BitConverter.GetBytes(value));
    }
    public static void Write(IntPtr address, bool value)
    {
        Write(address, BitConverter.GetBytes(value));
    }
    public static void Write(UIntPtr address, bool value)
    {
        Write(address, BitConverter.GetBytes(value));
    }


    public static void Write(int address, string value)
    {
        Write(address, Encoding.ASCII.GetBytes(value + (char)0));
    }
    public static void Write(uint address, string value)
    {
        Write(address, Encoding.ASCII.GetBytes(value + (char)0));
    }
    public static void Write(IntPtr address, string value)
    {
        Write(address, Encoding.ASCII.GetBytes(value + (char)0));
    }
    public static void Write(UIntPtr address, string value)
    {
        Write(address, Encoding.ASCII.GetBytes(value + (char)0));
    }


    public static void Write(int address, byte[] bytes)
    {
        Write((UIntPtr)address, bytes);
    }
    public static void Write(uint address, byte[] bytes)
    {
        Write((UIntPtr)address, bytes);
    }
    public static void Write(IntPtr address, byte[] bytes)
    {
        Write((UIntPtr)(int)address, bytes);
    }
    public static void Write(UIntPtr address, byte[] bytes)
    {
        writeSuccess = WriteProcessMemory(processHandle, address, bytes, bytes.Length, UIntPtr.Zero);
    }


    public static int Allocate(int address, int length, int access = 0x40)
    {
        return Allocate((UIntPtr)address, length, access);
    }
    public static int Allocate(uint address, int length, int access = 0x40)
    {
        return Allocate((UIntPtr)address, length, access);
    }
    public static int Allocate(IntPtr address, int length, int access = 0x40)
    {
        return Allocate((UIntPtr)(int)address, length, access);
    }
    public static int Allocate(UIntPtr address, int length, int access = 0x40)
    {
        return VirtualAlloc(address, length, 0x3000, access);
    }

    public static bool Free(int address, int length)
    {
        return Free((UIntPtr)address, length);
    }
    public static bool Free(uint address, int length)
    {
        return Free((UIntPtr)address, length);
    }
    public static bool Free(IntPtr address, int length)
    {
        return Free((UIntPtr)(int)address, length);
    }
    public static bool Free(UIntPtr address, int length)
    {
        return VirtualFree(address, length, 0x8000); // 0x8000 = MEM_RELEASE
    }
}

