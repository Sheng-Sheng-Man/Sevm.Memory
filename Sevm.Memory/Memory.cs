using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Sevm {

    /// <summary>
    /// 存储空间
    /// </summary>
    public unsafe class Memory : IDisposable {

        // 分页大小
        private const int Page_Size = 4096;

        /// <summary>
        /// 字节类型所占空间
        /// </summary>
        public const int Byte_Size = 1;

        /// <summary>
        /// 整型类型所占空间
        /// </summary>
        public const int Integer_Size = 4;

        /// <summary>
        /// 长整型类型所占空间
        /// </summary>
        public const int Long_Size = 8;

        /// <summary>
        /// 单精度类型所占空间
        /// </summary>
        public const int Float_Size = 4;

        /// <summary>
        /// 双精度类型所占空间
        /// </summary>
        public const int Double_Size = 8;

        /// <summary>
        /// 列表所占空间
        /// </summary>
        public const int List_Size = 20;

        /// <summary>
        /// 列表项目所占空间
        /// </summary>
        public const int List_Item_Size = 16;

        /// <summary>
        /// 对象所占空间
        /// </summary>
        public const int Object_Size = 16;

        /// <summary>
        /// 函数所占空间
        /// </summary>
        public const int Function_Size = 8;

        /// <summary>
        /// 原生对象所占空间
        /// </summary>
        public const int NativeObject_Size = 4;

        /// <summary>
        /// 原生函数所占空间
        /// </summary>
        public const int NativeFunction_Size = 4;

        /// <summary>
        /// 获取内存类型占用空间
        /// </summary>
        /// <param name="tp"></param>
        public static int GetMemoryTypeSize(MemoryTypes tp) {
            switch (tp) {
                case MemoryTypes.Byte: return Byte_Size;
                case MemoryTypes.Integer: return Integer_Size;
                case MemoryTypes.Long: return Long_Size;
                case MemoryTypes.Float: return Float_Size;
                case MemoryTypes.Double: return Double_Size;
                case MemoryTypes.List: return List_Size;
                case MemoryTypes.ListItem: return List_Item_Size;
                case MemoryTypes.Object: return Object_Size;
                case MemoryTypes.Function: return Function_Size;
                case MemoryTypes.NativeObject: return NativeObject_Size;
                case MemoryTypes.NativeFunction: return NativeFunction_Size;
                default: return 0;
            }
        }

        // 内存分页
        private List<IntPtr> pages;

        /// <summary>
        /// 所占内存
        /// </summary>
        public int SpaceOccupied { get; private set; }

        /// <summary>
        /// 当前分页指针
        /// </summary>
        public IntPtr IntPtr { get; private set; }

        /// <summary>
        /// 当前分页新地址偏移
        /// </summary>
        public int Offset { get; private set; }

        /// <summary>
        /// 当前分页尺寸
        /// </summary>
        public int Size { get; private set; }

        /// <summary>
        /// 对象实例化
        /// </summary>
        public Memory() {
            pages = new List<IntPtr>();
            this.IntPtr = IntPtr.Zero;
            this.Offset = 0;
        }

        // 创建内存页
        private void CreatePage(int size = Page_Size) {
            // 申请内存空间
            IntPtr ptr = Marshal.AllocHGlobal(size);
            // 设置相关参数
            this.IntPtr = ptr;
            this.Offset = 0;
            this.Size = size;
            this.SpaceOccupied += size;
            // 添加到列表
            pages.Add(ptr);
            Debug.WriteLine($"CreatePage:{ptr}(0x{ptr.ToString("x")})-{ptr + size}(0x{(ptr + size).ToString("x")})");
        }

        // 检测当前内存页是否具有足够的空间，当空间不够时，就创建新的内存页
        internal void CheckAndCreatePage(int size) {
            if (this.Offset + size > this.Size) {
                if (size > Page_Size) {
                    CreatePage(size);
                } else {
                    CreatePage();
                }
            }
        }

        /// <summary>
        /// 创建一个空内存
        /// </summary>
        /// <returns></returns>
        public MemoryPtr Create() {
            // 定义返回指针描述
            MemoryPtr res = new MemoryPtr() {
                Type = MemoryTypes.None,
                Size = 0,
            };
            // 定义所需的内存大小并检测
            int size = 1;
            CheckAndCreatePage(size);
            // 设置数据执政
            byte* tp = (byte*)(this.IntPtr + this.Offset);
            // 设置数据
            *tp = (byte)res.Type;
            // 设置结果指针并修改偏移
            res.IntPtr = this.IntPtr + this.Offset;
            this.Offset += size;
            // 返回结果
            return res;
        }

        #region [=====设置内容=====]

        /// <summary>
        /// 设置内容
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void Set(MemoryPtr ptr, double value) {
            // 目标为整型，也直接赋值
            if (ptr.Type == MemoryTypes.Value) {
                ptr.Content = (long)value;
                return;
            }
            // 目标为双精度，则直接赋值
            if (ptr.Type == MemoryTypes.Double) {
                *ptr.DoublePtr = value;
                return;
            }
            // 目标为整型，也直接赋值
            if (ptr.Type == MemoryTypes.Integer) {
                *ptr.IntegerPtr = (int)value;
                return;
            }
            // 否则创建一个新的虚拟内存
            var res = CreateDouble(value);
            ptr.Type = res.Type;
            ptr.Size = res.Size;
            ptr.IntPtr = res.IntPtr;
            ptr.DoublePtr = res.DoublePtr;
        }

        /// <summary>
        /// 设置内容
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void Set(MemoryPtr ptr, string value) {
            var res = CreateString(value);
            ptr.Type = res.Type;
            ptr.Size = res.Size;
            ptr.IntPtr = res.IntPtr;
        }

        /// <summary>
        /// 设置内容
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void Set(MemoryPtr ptr, MemoryPtr value) {
            // 判断类型
            switch (ptr.Type) {
                case MemoryTypes.None:
                    #region [=====目标为空=====]
                    switch (value.Type) {
                        case MemoryTypes.Integer:
                            Set(ptr, (double)(*value.IntegerPtr));
                            return;
                        case MemoryTypes.Double:
                            Set(ptr, *value.DoublePtr);
                            return;
                        case MemoryTypes.Value:
                            Set(ptr, (double)value.Content);
                            return;
                        case MemoryTypes.None:
                        case MemoryTypes.String:
                        case MemoryTypes.List:
                        case MemoryTypes.Object:
                        case MemoryTypes.NativeObject:
                        case MemoryTypes.NativeFunction:
                            ptr.Type = value.Type;
                            ptr.Size = value.Size;
                            ptr.IntPtr = value.IntPtr;
                            return;
                        default: throw new Exception($"尚未支持的源数据类型'{value.Type.ToString()}'");
                    }
                #endregion
                case MemoryTypes.Value:
                    #region [=====目标为值类型=====]
                    switch (value.Type) {
                        case MemoryTypes.Integer:
                            ptr.Content = *value.IntegerPtr;
                            return;
                        case MemoryTypes.Double:
                            ptr.Content = (long)*value.DoublePtr;
                            return;
                        case MemoryTypes.Value:
                            ptr.Content = value.Content;
                            return;
                        default: throw new Exception($"尚未支持数据类型'{value.Type.ToString()}'赋值给数据类型'{ptr.Type.ToString()}'");
                    }
                #endregion
                case MemoryTypes.Integer:
                    #region [=====目标为整型=====]
                    switch (value.Type) {
                        case MemoryTypes.Integer:
                            *ptr.IntegerPtr = *value.IntegerPtr;
                            return;
                        case MemoryTypes.Double:
                            *ptr.IntegerPtr = (int)*value.DoublePtr;
                            return;
                        case MemoryTypes.Value:
                            *ptr.IntegerPtr = (int)value.Content;
                            return;
                        default: throw new Exception($"尚未支持数据类型'{value.Type.ToString()}'赋值给数据类型'{ptr.Type.ToString()}'");
                    }
                #endregion
                case MemoryTypes.Double:
                    #region [=====目标为双精度数据=====]
                    switch (value.Type) {
                        case MemoryTypes.Integer:
                            *ptr.DoublePtr = *value.IntegerPtr;
                            return;
                        case MemoryTypes.Double:
                            *ptr.DoublePtr = *value.DoublePtr;
                            return;
                        case MemoryTypes.Value:
                            *ptr.DoublePtr = value.Content;
                            return;
                        default: throw new Exception($"尚未支持数据类型'{value.Type.ToString()}'赋值给数据类型'{ptr.Type.ToString()}'");
                    }
                #endregion
                case MemoryTypes.String:
                    #region [=====目标为字符串=====]
                    switch (value.Type) {
                        case MemoryTypes.Integer:
                            Set(ptr, (double)(*value.IntegerPtr));
                            return;
                        case MemoryTypes.Double:
                            Set(ptr, *value.DoublePtr);
                            return;
                        case MemoryTypes.String:
                            Set(ptr, value.GetString());
                            return;
                        default: throw new Exception($"尚未支持数据类型'{value.Type.ToString()}'赋值给数据类型'{ptr.Type.ToString()}'");
                    }
                #endregion
                default: throw new Exception($"尚未支持的目标数据类型'{ptr.Type.ToString()}'");
            }
        }

        #endregion

        #region [=====字符串处理相关操作=====]

        /// <summary>
        /// 创建一个字符串内存
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public MemoryPtr CreateString(string content) {
            // 创建UTF8字符串
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
            // 定义返回指针描述
            MemoryPtr res = new MemoryPtr() {
                Type = MemoryTypes.String,
                Size = bytes.Length + 4,
            };
            // 定义所需的内存大小并检测
            int size = res.Size + 1;
            CheckAndCreatePage(size);
            // 设置数据执政
            byte* tp = (byte*)(this.IntPtr + this.Offset);
            int* len = (int*)(this.IntPtr + this.Offset + 1);
            // 设置数据
            *tp = (byte)res.Type;
            *len = bytes.Length;
            Marshal.Copy(bytes, 0, this.IntPtr + this.Offset + 5, bytes.Length);
            // 设置结果指针并修改偏移
            res.IntPtr = this.IntPtr + this.Offset;
            this.Offset += size;
            // 清理指针
            tp = null;
            len = null;
            // 调试输出
            Debug.WriteLine($"CreateString:{res.Type.ToString()}[{res.Size}] {res.IntPtr}(0x{res.IntPtr.ToString("x")})");
            // 返回结果
            return res;
        }

        #endregion

        #region [=====整型相关操作=====]

        /// <summary>
        /// 创建一个整型数据内存
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public MemoryPtr CreateInteger(int content) {
            // 定义返回指针描述
            MemoryPtr res = new MemoryPtr() {
                Type = MemoryTypes.Integer,
                Size = Integer_Size,
            };
            // 定义所需的内存大小并检测
            int size = res.Size + 1;
            CheckAndCreatePage(size);
            // 设置数据执政
            byte* tp = (byte*)(this.IntPtr + this.Offset);
            res.IntegerPtr = (int*)(this.IntPtr + this.Offset + 1);
            // 设置数据
            *tp = (byte)res.Type;
            *res.IntegerPtr = content;
            // 设置结果指针并修改偏移
            res.IntPtr = this.IntPtr + this.Offset;
            this.Offset += size;
            // 清理指针
            tp = null;
            // 调试输出
            Debug.WriteLine($"CreateInteger:{res.Type.ToString()}[{res.Size}] {res.IntPtr}(0x{res.IntPtr.ToString("x")})");
            // 返回结果
            return res;
        }

        #endregion

        #region [=====长整型相关操作=====]

        /// <summary>
        /// 创建一个整型数据内存
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public MemoryPtr Createlong(long content) {
            // 定义返回指针描述
            MemoryPtr res = new MemoryPtr() {
                Type = MemoryTypes.Long,
                Size = Long_Size,
            };
            // 定义所需的内存大小并检测
            int size = res.Size + 1;
            CheckAndCreatePage(size);
            // 设置数据执政
            byte* tp = (byte*)(this.IntPtr + this.Offset);
            res.LongPtr = (long*)(this.IntPtr + this.Offset + 1);
            // 设置数据
            *tp = (byte)res.Type;
            *res.LongPtr = content;
            // 设置结果指针并修改偏移
            res.IntPtr = this.IntPtr + this.Offset;
            this.Offset += size;
            // 清理指针
            tp = null;
            // 调试输出
            Debug.WriteLine($"Createlong:{res.Type.ToString()}[{res.Size}] {res.IntPtr}(0x{res.IntPtr.ToString("x")})");
            // 返回结果
            return res;
        }

        #endregion

        #region [=====双进度类型相关操作=====]

        /// <summary>
        /// 创建一个双精度数据内存
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public MemoryPtr CreateDouble(double content) {
            // 定义返回指针描述
            MemoryPtr res = new MemoryPtr() {
                Type = MemoryTypes.Double,
                Size = Double_Size,
            };
            // 定义所需的内存大小并检测
            int size = Double_Size + 1;
            CheckAndCreatePage(size);
            // 设置数据执政
            byte* tp = (byte*)(this.IntPtr + this.Offset);
            res.DoublePtr = (double*)(this.IntPtr + this.Offset + 1);
            // 设置数据
            *tp = (byte)res.Type;
            *res.DoublePtr = content;
            // 设置结果指针并修改偏移
            res.IntPtr = this.IntPtr + this.Offset;
            this.Offset += size;
            // 清理指针
            tp = null;
            // 调试输出
            Debug.WriteLine($"CreateDouble:{res.Type.ToString()}[{res.Size}] {res.IntPtr}(0x{res.IntPtr.ToString("x")})");
            // 返回结果
            return res;
        }

        #endregion

        #region [=====列表处理相关操作=====]

        /// <summary>
        /// 创建一个列表
        /// </summary>
        /// <returns></returns>
        public MemoryPtr CreateList() {
            // 定义返回指针描述
            MemoryPtr res = new MemoryPtr() {
                Type = MemoryTypes.List,
                Size = List_Size,
            };
            // 定义所需的内存大小并检测
            int size = res.Size + 1;
            CheckAndCreatePage(size);
            // 设置数据指针
            byte* tp = (byte*)(this.IntPtr + this.Offset);
            long* firstAddr = (long*)(this.IntPtr + this.Offset + 1);
            long* lastAddr = (long*)(this.IntPtr + this.Offset + 9);
            int* count = (int*)(this.IntPtr + this.Offset + 17);
            // 设置数据
            *tp = (byte)res.Type;
            *firstAddr = 0;
            *lastAddr = 0;
            *count = 0;
            // 设置结果指针并修改偏移
            res.IntPtr = this.IntPtr + this.Offset;
            this.Offset += size;
            Debug.WriteLine($"CreateList:{res.Type.ToString()}[{res.Size}] {res.IntPtr}(0x{res.IntPtr.ToString("x")})");
            // 清理指针
            tp = null;
            firstAddr = null;
            lastAddr = null;
            count = null;
            // 返回结果
            return res;
        }

        /// <summary>
        /// 创建列表项目
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="ptrc"></param>
        /// <returns></returns>
        public MemoryPtr AddListItem(MemoryPtr ptr, MemoryPtr ptrc) {
            // 定义返回指针描述
            MemoryPtr res = new MemoryPtr() {
                Type = MemoryTypes.ListItem,
                Size = Memory.List_Item_Size,
            };
            // 定义所需的内存大小并检测
            int size = res.Size + 1;
            CheckAndCreatePage(size);
            // 设置数据指针
            long* firstAddr = (long*)(ptr.IntPtr + 1);
            long* lastAddr = (long*)(ptr.IntPtr + 9);
            int* count = (int*)(ptr.IntPtr + 17);
            byte* tp = (byte*)(this.IntPtr + this.Offset);
            long* contentAddr = (long*)(this.IntPtr + this.Offset + 1);
            long* nextAddr = (long*)(this.IntPtr + this.Offset + 9);
            // 设置数据
            *tp = (byte)res.Type;
            switch (ptrc.Type) {
                case MemoryTypes.None:
                    *contentAddr = 0;
                    break;
                case MemoryTypes.Value:
                    *contentAddr = (long)CreateDouble((double)ptrc.IntPtr).IntPtr;
                    break;
                default:
                    *contentAddr = (long)ptrc.IntPtr;
                    break;
            }
            *nextAddr = 0;
            // 设置结果指针并修改偏移
            res.IntPtr = this.IntPtr + this.Offset;
            // 判断是否为列表中的第一项
            if (*firstAddr == 0) {
                *firstAddr = (long)res.IntPtr;
                *lastAddr = (long)res.IntPtr;
                *count = 1;
            } else {
                // 读取最后一个项目
                IntPtr ptrl = new IntPtr(*lastAddr);
                long* ptrlNextAddr = (long*)(ptrl + 9);
                // 修改指向
                *ptrlNextAddr = (long)res.IntPtr;
                *lastAddr = (long)res.IntPtr;
                *count += 1;
            }
            this.Offset += size;
            // 返回结果
            return res;
        }

        /// <summary>
        /// 创建列表项目
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public MemoryPtr AddListItem(MemoryPtr ptr) {
            return AddListItem(ptr, Sevm.MemoryPtr.None);
        }

        #endregion

        #region [=====对象处理相关操作=====]

        /// <summary>
        /// 创建一个对象
        /// </summary>
        /// <returns></returns>
        public MemoryPtr CreateObject() {
            // 定义返回指针描述
            MemoryPtr res = new MemoryPtr() {
                Type = MemoryTypes.Object,
                Size = Object_Size,
            };
            // 定义所需的内存大小并检测
            int size = res.Size + 1;
            CheckAndCreatePage(size);
            // 创建关联列表
            MemoryPtr keys = CreateList();
            MemoryPtr values = CreateList();
            // 设置数据指针
            byte* tp = (byte*)(this.IntPtr + this.Offset);
            long* keyAddr = (long*)(this.IntPtr + this.Offset + 1);
            long* valueAddr = (long*)(this.IntPtr + this.Offset + 9);
            // 设置数据
            *tp = (byte)res.Type;
            *keyAddr = (long)keys.IntPtr;
            *valueAddr = (long)values.IntPtr;
            // 设置结果指针并修改偏移
            res.IntPtr = this.IntPtr + this.Offset;
            this.Offset += size;
            // 清理指针
            tp = null;
            keyAddr = null;
            valueAddr = null;
            Debug.WriteLine($"CreateObject:{res.Type.ToString()}[{res.Size}] {res.IntPtr}(0x{res.IntPtr.ToString("x")})");
            // 返回结果
            return res;
        }

        #endregion

        #region [=====函数相关操作=====]

        /// <summary>
        /// 创建一个函数对象
        /// </summary>
        /// <param name="library"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public MemoryPtr CreateFunction(int library, int index) {
            // 定义返回指针描述
            MemoryPtr res = new MemoryPtr() {
                Type = MemoryTypes.Function,
                Size = Function_Size,
            };
            // 定义所需的内存大小并检测
            int size = res.Size + 1;
            CheckAndCreatePage(size);
            // 设置数据指针
            byte* tp = (byte*)(this.IntPtr + this.Offset);
            int* pLibrary = (int*)(this.IntPtr + this.Offset + 1);
            int* pIndex = (int*)(this.IntPtr + this.Offset + 5);
            // 设置数据
            *tp = (byte)res.Type;
            *pLibrary = library;
            *pIndex = index;
            // 设置结果指针并修改偏移
            res.IntPtr = this.IntPtr + this.Offset;
            this.Offset += size;
            // 清空指针
            tp = null;
            pLibrary = null;
            pIndex = null;
            // 调试输出
            Debug.WriteLine($"CreateFunction:{res.Type.ToString()}[{res.Size}] {res.IntPtr}(0x{res.IntPtr.ToString("x")})");
            // 返回结果
            return res;
        }

        /// <summary>
        /// 获取库文件索引
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public int GetFunctionLibrary(MemoryPtr ptr) {
            // 设置数据指针
            int* library = (int*)(ptr.IntPtr + 1);
            return *library;
        }

        /// <summary>
        /// 获取函数索引
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public int GetFunctionIndex(MemoryPtr ptr) {
            // 设置数据指针
            int* index = (int*)(ptr.IntPtr + 5);
            return *index;
        }

        #endregion

        #region [=====原生相关操作=====]

        /// <summary>
        /// 创建一个原生对象
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public MemoryPtr CreateNativeObject(int index) {
            // 定义返回指针描述
            MemoryPtr res = new MemoryPtr() {
                Type = MemoryTypes.NativeObject,
                Size = NativeObject_Size,
            };
            // 定义所需的内存大小并检测
            int size = res.Size + 1;
            CheckAndCreatePage(size);
            // 设置数据执政
            byte* tp = (byte*)(this.IntPtr + this.Offset);
            res.IntegerPtr = (int*)(this.IntPtr + this.Offset + 1);
            // 设置数据
            *tp = (byte)res.Type;
            *res.IntegerPtr = index;
            // 设置结果指针并修改偏移
            res.IntPtr = this.IntPtr + this.Offset;
            this.Offset += size;
            // 调试输出
            Debug.WriteLine($"CreateNativeObject:{res.Type.ToString()}[{res.Size}] {res.IntPtr}(0x{res.IntPtr.ToString("x")})");
            // 返回结果
            return res;
        }

        /// <summary>
        /// 创建一个原生函数
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public MemoryPtr CreateNativeFunction(int index) {
            // 定义返回指针描述
            MemoryPtr res = new MemoryPtr() {
                Type = MemoryTypes.NativeFunction,
                Size = NativeFunction_Size,
            };
            // 定义所需的内存大小并检测
            int size = res.Size + 1;
            CheckAndCreatePage(size);
            // 设置数据执政
            byte* tp = (byte*)(this.IntPtr + this.Offset);
            res.IntegerPtr = (int*)(this.IntPtr + this.Offset + 1);
            // 设置数据
            *tp = (byte)res.Type;
            *res.IntegerPtr = index;
            // 设置结果指针并修改偏移
            res.IntPtr = this.IntPtr + this.Offset;
            this.Offset += size;
            // 调试输出
            Debug.WriteLine($"CreateNativeFunction:{res.Type.ToString()}[{res.Size}] {res.IntPtr}(0x{res.IntPtr.ToString("x")})");
            // 返回结果
            return res;
        }

        #endregion

        /// <summary>
        /// // 释放资源
        /// </summary>
        public void Dispose() {
            //throw new NotImplementedException();
            for (int i = 0; i < pages.Count; i++) {
                Marshal.FreeHGlobal(pages[i]);
            }
            pages.Clear();
            pages = null;
        }

    }
}
