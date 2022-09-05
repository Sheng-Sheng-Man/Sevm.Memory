using System;
using System.Collections.Generic;
using System.Text;

namespace Sevm {

    /// <summary>
    /// 列表
    /// </summary>
    public unsafe class MemoryList : IDisposable {

        // 指针定义
        private int* count;
        private long* firstAddr;
        private long* lastAddr;

        // 列表指针缓存
        private List<MemoryPtr> items;
        private List<MemoryPtr> values;

        // 获取列表的第一个项目
        private MemoryPtr GetFirstItem() {
            // 定义返回指针描述
            MemoryPtr res = new MemoryPtr() {
                Type = MemoryTypes.ListItem,
                Size = Memory.List_Item_Size,
            };
            // 内容为空的情况
            if (*firstAddr == 0) throw new Exception($"数组越界");
            // 设置数据
            res.IntPtr = new IntPtr(*firstAddr);
            // 返回结果
            return res;
        }

        // 获取列表的下一个项目
        private MemoryPtr GetNextItem(MemoryPtr ptrItem) {
            // 定义返回指针描述
            MemoryPtr res = new MemoryPtr() {
                Type = MemoryTypes.ListItem,
                Size = Memory.List_Item_Size,
            };
            // 设置数据指针
            long* nextAddr = (long*)(ptrItem.IntPtr + 9);
            // 内容为空的情况
            if (*nextAddr == 0) throw new Exception($"数组越界");
            // 设置数据
            res.IntPtr = new IntPtr(*nextAddr);
            // 返回结果
            return res;
        }

        // 获取列表项的内容指针
        private MemoryPtr GetItemContent(MemoryPtr ptrItem) {
            // 定义返回指针描述
            MemoryPtr res = new MemoryPtr();
            // 设置数据指针
            long* contentAddr = (long*)(ptrItem.IntPtr + 1);
            // 内容为空的情况
            if (*contentAddr == 0) {
                res.Type = MemoryTypes.None;
                res.Size = 0;
                res.IntPtr = IntPtr.Zero;
                // 返回结果
                return res;
            }
            // 返回结果
            return MemoryPtr.CreateFromAddr(*contentAddr);
        }

        // 设置列表项的内容
        private void SetItemContent(MemoryPtr ptrItem, IntPtr value) {
            // 设置数据指针
            long* contentAddr = (long*)(ptrItem.IntPtr + 1);
            *contentAddr = (long)value;
            // 刷新缓存
            this.RefreshCache();
        }

        // 刷新缓存数据
        private void RefreshCache() {
            // 列表清理
            items.Clear();
            values.Clear();
            // 当列表为空的时候，直接退出
            if (this.Count <= 0) return;
            // 处理第一个项目
            MemoryPtr item = GetFirstItem();
            MemoryPtr value = GetItemContent(item);
            items.Add(item);
            values.Add(value);
            // 处理剩下的项目
            for (int i = 1; i < this.Count; i++) {
                item = GetNextItem(item);
                value = GetItemContent(item);
                items.Add(item);
                values.Add(value);
            }
        }

        /// <summary>
        /// 获取存储管理器
        /// </summary>
        public Memory Memory { get; private set; }

        /// <summary>
        /// 获取列表指针
        /// </summary>
        public MemoryPtr MemoryPtr { get; private set; }

        /// <summary>
        /// 获取项目缓存数量
        /// </summary>
        public int ItemsCacheCount { get { return items.Count; } }

        /// <summary>
        /// 获取内容缓存数量
        /// </summary>
        public int ValuesCacheCount { get { return values.Count; } }

        /// <summary>
        /// 获取数量统计
        /// </summary>
        public int Count { get { return *count; } }

        /// <summary>
        /// 实例化一个列表
        /// </summary>
        /// <param name="memory"></param>
        /// <param name="ptr"></param>
        public MemoryList(Memory memory, MemoryPtr ptr) {
            this.Memory = memory;
            this.MemoryPtr = ptr;
            items = new List<MemoryPtr>();
            values = new List<MemoryPtr>();
            // 设置数据指针
            firstAddr = (long*)(ptr.IntPtr + 1);
            lastAddr = (long*)(ptr.IntPtr + 9);
            count = (int*)(ptr.IntPtr + 17);
            // 刷新缓存
            this.RefreshCache();
        }

        /// <summary>
        /// 将列表转化为字符串
        /// </summary>
        /// <returns></returns>
        public string ConvertListToString() {
            if (this.Count <= 0) return null;
            // 定义字符串
            StringBuilder sb = new StringBuilder();
            // 拼接字符串
            for (int i = 0; i < this.Count; i++) {
                var value = values[i];
                if (value.Type != MemoryTypes.None) {
                    sb.Append(value.GetString());
                }
            }
            // 返回结果
            return sb.ToString();
        }

        /// <summary>
        /// 添加项目
        /// </summary>
        /// <returns></returns>
        public MemoryPtr AddItem() {
            // 添加子项目
            MemoryPtr res = this.Memory.AddListItem(this.MemoryPtr);
            // 刷新缓存
            this.RefreshCache();
            return res;
        }

        /// <summary>
        /// 添加项目
        /// </summary>
        /// <returns></returns>
        public MemoryPtr AddItem(MemoryPtr ptr) {
            // 添加子项目
            MemoryPtr res = this.Memory.AddListItem(this.MemoryPtr, ptr);
            // 刷新缓存
            this.RefreshCache();
            return res;
        }

        /// <summary>
        /// 获取列表的项目
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public MemoryPtr GetItem(int index) {
            // 判读数组越界
            if (this.Count <= index) throw new Exception($"数组越界");
            // 返回结果
            return items[index];
        }

        /// <summary>
        /// 获取列表项的内容
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public MemoryPtr GetItemContent(int index) {
            // 判读数组越界
            if (this.Count <= index) throw new Exception($"数组越界");
            // 返回结果
            return values[index];
        }

        /// <summary>
        /// 设置列表项的内容
        /// </summary>
        /// <param name="ptrItem"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetItemContent(MemoryPtr ptrItem, MemoryPtr value) {
            SetItemContent(ptrItem, value.IntPtr);
        }

        /// <summary>
        /// 设置列表项的内容
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetItemContent(int index, IntPtr value) {
            // 定义返回指针描述
            MemoryPtr ptrc = GetItem(index);
            // 设置内容
            SetItemContent(ptrc, value);
        }

        /// <summary>
        /// 设置列表项的内容
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetItemContent(int index, MemoryPtr value) {
            SetItemContent(index, value.IntPtr);
        }

        /// <summary>
        /// 根据内容获取索引
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetIndex(string value) {
            // 遍历项目
            for (int i = 0; i < this.Count; i++) {
                var itemValue = values[i];
                if (itemValue.Type != MemoryTypes.None) {
                    if (itemValue.GetString() == value) return i;
                }
            }
            // 未找到匹配内容，则返回-1
            return -1;
        }

        /// <summary>
        /// 根据内容获取索引
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetIndex(double value) {
            // 遍历项目
            for (int i = 0; i < this.Count; i++) {
                var itemValue = values[i];
                if (itemValue.Type != MemoryTypes.None) {
                    if (itemValue.GetDouble() == value) return i;
                }
            }
            // 未找到匹配内容，则返回-1
            return -1;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose() {
            //throw new NotImplementedException();
            count = null;
            firstAddr = null;
            lastAddr = null;
        }
    }
}
