using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Sevm {

    /// <summary>
    /// 虚拟内存指针
    /// </summary>
    public unsafe class MemoryPtr : IDisposable {

        /// <summary>
        /// 数据类型
        /// </summary>
        public MemoryTypes Type { get; set; }

        /// <summary>
        /// 数据大小
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// 指针地址
        /// </summary>
        public IntPtr IntPtr { get; set; }

        /// <summary>
        /// 相关的整型指针
        /// </summary>
        public int* IntegerPtr;

        /// <summary>
        /// 相关的长整型指针
        /// </summary>
        public long* LongPtr;

        /// <summary>
        /// 相关的双进度指针
        /// </summary>
        public double* DoublePtr;

        /// <summary>
        /// 对象实例化
        /// </summary>
        public MemoryPtr() {
            this.Type = MemoryTypes.None;
            this.Size = 0;
            this.IntPtr = IntPtr.Zero;
        }

        /// <summary>
        /// 获取一个空指针
        /// </summary>
        public static MemoryPtr None { get { return new MemoryPtr() { Type = MemoryTypes.None, Size = 0, IntPtr = IntPtr.Zero }; } }

        /// <summary>
        /// 获取一个数值伪指针
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static MemoryPtr Value(long value) { return new MemoryPtr() { Type = MemoryTypes.Value, Size = 0, IntPtr = new IntPtr(value) }; }

        /// <summary>
        /// 从数值伪指针中获取值
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public static long GetValue(MemoryPtr ptr) {
            if (ptr.Type != MemoryTypes.Value) throw new Exception($"指针类型'{ptr.Type.ToString()}'无法直接获取值");
            return (long)ptr.IntPtr;
        }

        /// <summary>
        /// 从整型数据建立对象
        /// </summary>
        /// <param name="value">内容</param>
        public static implicit operator MemoryPtr(long value) {
            return MemoryPtr.Value(value);
        }

        /// <summary>
        /// 获取虚拟内存专用指针
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public static MemoryPtr CreateFromIntPtr(IntPtr ptr) {
            // 定义返回指针描述
            MemoryPtr res = new MemoryPtr();
            // 设置数据
            res.IntPtr = ptr;
            byte* tp = (byte*)res.IntPtr;
            res.Type = (MemoryTypes)(*tp);
            // 计算数据长度
            if (res.Type == MemoryTypes.String) {
                int* len = (int*)(res.IntPtr + 1);
                res.Size = *len + 4;
            } else {
                res.Size = Memory.GetMemoryTypeSize(res.Type);
                // 添加指针
                if (res.Type == MemoryTypes.Integer) res.IntegerPtr = (int*)(res.IntPtr + 1);
                //if (res.Type == MemoryTypes.Function) res.IntegerPtr = (int*)(res.IntPtr + 1);
                if (res.Type == MemoryTypes.NativeObject) res.IntegerPtr = (int*)(res.IntPtr + 1);
                if (res.Type == MemoryTypes.NativeFunction) res.IntegerPtr = (int*)(res.IntPtr + 1);
                if (res.Type == MemoryTypes.Double) res.DoublePtr = (double*)(res.IntPtr + 1);
            }
            // 返回结果
            return res;
        }

        /// <summary>
        /// 获取虚拟内存专用指针
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public static MemoryPtr CreateFromAddr(long addr) {
            return CreateFromIntPtr(new IntPtr(addr));
        }

        /// <summary>
        /// 获取整型数据
        /// </summary>
        /// <returns></returns>
        public int GetInteger() {
            if (this.Type == MemoryTypes.Double) return (int)(*this.DoublePtr);
            if (this.Type == MemoryTypes.Long) return (int)*this.LongPtr;
            if (this.Type == MemoryTypes.Value) return (int)this.IntPtr;
            return *this.IntegerPtr;
        }

        /// <summary>
        /// 设置整型数据
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public void SetInteger(int content) { *this.IntegerPtr = content; }

        /// <summary>
        /// 获取字符串表示形式
        /// </summary>
        /// <returns></returns>
        public string GetString() {
            // 判断数据类型
            byte* tp = (byte*)this.IntPtr;
            MemoryTypes memoryType = (MemoryTypes)(*tp);
            switch (memoryType) {
                case MemoryTypes.Integer: return this.IntegerPtr->ToString();
                case MemoryTypes.Double: return this.DoublePtr->ToString();
                case MemoryTypes.String:
                    // 创建UTF8字符串
                    byte[] bytes = new byte[this.Size - 4];
                    // 从内存中复制数组
                    Marshal.Copy(this.IntPtr + 5, bytes, 0, this.Size - 4);
                    // 返回结果
                    return System.Text.Encoding.UTF8.GetString(bytes);
                default: throw new Exception($"尚未支持的源数据类型'{memoryType.ToString()}'");
            }
        }

        /// <summary>
        /// 获取整型数据
        /// </summary>
        /// <returns></returns>
        public long GetLong() {
            if (this.Type == MemoryTypes.Double) return (long)(*this.DoublePtr);
            if (this.Type == MemoryTypes.Integer) return *this.IntegerPtr;
            if (this.Type == MemoryTypes.Value) return (long)this.IntPtr;
            return *this.IntegerPtr;
        }

        /// <summary>
        /// 设置整型数据
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public void SetLong(long content) { *this.LongPtr = content; }

        /// <summary>
        /// 获取双精度数据
        /// </summary>
        /// <returns></returns>
        public double GetDouble() {
            if (this.Type == MemoryTypes.Integer) return *this.IntegerPtr;
            if (this.Type == MemoryTypes.Long) return *this.LongPtr;
            if (this.Type == MemoryTypes.Value) return (double)this.IntPtr;
            return *this.DoublePtr;
        }

        /// <summary>
        /// 设置双精度数据
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public void SetDouble(double content) { *this.DoublePtr = content; }

        #region [=====运算处理=====]

        /// <summary>
        /// 加法运算
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public void Add(double content) { *this.DoublePtr += content; }

        /// <summary>
        /// 加法运算
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public void Add(MemoryPtr ptr) {
            switch (ptr.Type) {
                case MemoryTypes.Integer:
                    *this.DoublePtr += *ptr.IntegerPtr;
                    break;
                case MemoryTypes.Long:
                    *this.DoublePtr += *ptr.LongPtr;
                    break;
                case MemoryTypes.Double:
                    *this.DoublePtr += *ptr.DoublePtr;
                    break;
                default: throw new Exception($"指针类型'{this.Type.ToString()}'不支持运算");
            }
        }

        /// <summary>
        /// 减法运算
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public void Sub(double content) { *this.DoublePtr -= content; }

        /// <summary>
        /// 减法运算
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public void Sub(MemoryPtr ptr) {
            switch (ptr.Type) {
                case MemoryTypes.Integer:
                    *this.DoublePtr -= *ptr.IntegerPtr;
                    break;
                case MemoryTypes.Long:
                    *this.DoublePtr -= *ptr.LongPtr;
                    break;
                case MemoryTypes.Double:
                    *this.DoublePtr -= *ptr.DoublePtr;
                    break;
                default: throw new Exception($"指针类型'{this.Type.ToString()}'不支持运算");
            }
        }

        /// <summary>
        /// 乘法运算
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public void Mul(double content) { *this.DoublePtr *= content; }

        /// <summary>
        /// 乘法运算
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public void Mul(MemoryPtr ptr) {
            switch (ptr.Type) {
                case MemoryTypes.Integer:
                    *this.DoublePtr *= *ptr.IntegerPtr;
                    break;
                case MemoryTypes.Long:
                    *this.DoublePtr *= *ptr.LongPtr;
                    break;
                case MemoryTypes.Double:
                    *this.DoublePtr *= *ptr.DoublePtr;
                    break;
                default: throw new Exception($"指针类型'{this.Type.ToString()}'不支持运算");
            }
        }

        /// <summary>
        /// 乘法运算
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public void Div(double content) { *this.DoublePtr /= content; }

        /// <summary>
        /// 乘法运算
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public void Div(MemoryPtr ptr) {
            switch (ptr.Type) {
                case MemoryTypes.Integer:
                    *this.DoublePtr /= *ptr.IntegerPtr;
                    break;
                case MemoryTypes.Long:
                    *this.DoublePtr /= *ptr.LongPtr;
                    break;
                case MemoryTypes.Double:
                    *this.DoublePtr /= *ptr.DoublePtr;
                    break;
                default: throw new Exception($"指针类型'{this.Type.ToString()}'不支持运算");
            }
        }

        #endregion

        /// <summary>
        /// 获取对象
        /// </summary>
        /// <returns></returns>
        public MemoryObject GetObject(Memory memory) {
            if (this.Type == MemoryTypes.Object) return new MemoryObject(memory, this);
            throw new Exception($"指针类型'{this.Type.ToString()}'无法转化为对象");
        }

        /// <summary>
        /// 获取对象
        /// </summary>
        /// <returns></returns>
        public MemoryList GetList(Memory memory) {
            if (this.Type == MemoryTypes.List) return new MemoryList(memory, this);
            throw new Exception($"指针类型'{this.Type.ToString()}'无法转化为列表");
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose() {
            //throw new NotImplementedException();
            IntegerPtr = null;
            LongPtr = null;
            DoublePtr = null;
        }
    }
}
