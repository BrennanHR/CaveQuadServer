using System;
using System.Text;
public class MessageBuffer
{
    public byte[] Buffer { get; private set; }
    public int Size { get { return Buffer.Length; } }
    public int Position { get; set; } = 0;

    public MessageBuffer(byte[] buffer)
    {
        Buffer = buffer;
    }

    public void SeekStart()
    {
        Position = 0;
        Array.Clear(Buffer, 0, Buffer.Length);
    }

    public byte ReadByte()
    {
        var val = Buffer[Position];
        Position++;
        return val;
    }

    public sbyte ReadSByte()
    {
        var val = (sbyte)Buffer[Position];
        Position++;
        return val;
    }

    public ushort ReadUInt16()
    {
        var value = BitConverter.ToUInt16(Buffer, Position);
        Position += 2;
        return value;
    }

    public short ReadInt16()
    {
        var value = BitConverter.ToInt16(Buffer, Position);
        Position += 2;
        return value;
    }

    public uint ReadUInt32()
    {
        var value = BitConverter.ToUInt32(Buffer, Position);
        Position += 4;
        return value;
    }

    public int ReadInt32()
    {
        var value = BitConverter.ToInt32(Buffer, Position);
        Position += 4;
        return value;
    }

    public float ReadFloat()
    {
        var value = BitConverter.ToSingle(Buffer, Position);
        Position += 4;
        return value;
    }

    public double ReadDouble()
    {
        var value = BitConverter.ToDouble(Buffer, Position);
        Position += 8;
        return value;
    }

    public bool ReadBoolean()
    {
        var value = BitConverter.ToBoolean(Buffer, Position);
        Position++;
        return value;
    }

    public string ReadString()
    {
        int stringEnd = Array.IndexOf(Buffer, (byte)'\0', Position) + 1;
        var value = Encoding.UTF8.GetString(Buffer, Position, stringEnd - Position -1);
        Position += value.Length+1;
        return value;
    }

    //write functions

    public void WriteByte(byte value)
    {
        Buffer[Position] = value;
        Position++;
    }

    public void WriteSByte(sbyte value)
    {
        Buffer[Position] = (byte)value;
        Position++;
    }

    public void WriteUInt16(ushort value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        Array.Copy(bytes, 0, Buffer, Position, 2);
        Position += 2;
    }

    public void WriteInt16(short value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        Array.Copy(bytes, 0, Buffer, Position, 2);
        Position += 2;
    }

    public void WriteUInt32(uint value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        Array.Copy(bytes, 0, Buffer, Position, 4);
        Position += 4;
    }

    public void WriteInt32(int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        Array.Copy(bytes, 0, Buffer, Position, 4);
        Position += 4;
    }

    public void WriteFloat(float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        Array.Copy(bytes, 0, Buffer, Position, 4);
        Position += 4;
    }

    public void WriteDouble(double value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        Array.Copy(bytes, 0, Buffer, Position, 8);
        Position += 8;
    }

    public void WriteBoolean(bool value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        Array.Copy(bytes, 0, Buffer, Position, 1);
        Position += 1;
    }

    public void WriteString(string value)
    {
        value = value.Replace("\0", "\\0");
        byte[] bytes = Encoding.UTF8.GetBytes(value + '\0');
        Array.Copy(bytes, 0, Buffer, Position, bytes.Length);
        Position += bytes.Length;
    }

    public String toString()
    {
        return Encoding.UTF8.GetString(Buffer,0,Buffer.Length);
    }
}