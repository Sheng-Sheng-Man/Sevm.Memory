using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Sevm {

    /// <summary>
    /// 对象
    /// </summary>
    public unsafe class MemoryObject : IDisposable {

        // 指针定义
        private long* keyAddr;
        private long* valueAddr;

        /// <summary>
        /// 获取存储管理器
        /// </summary>
        public Memory Memory { get; private set; }

        /// <summary>
        /// 获取列表指针
        /// </summary>
        public MemoryPtr MemoryPtr { get; private set; }

        /// <summary>
        /// 获取键列表
        /// </summary>
        public MemoryList Keys { get; private set; }

        /// <summary>
        /// 获取值列表
        /// </summary>
        public MemoryList Values { get; private set; }

        /// <summary>
        /// 实例化一个列表
        /// </summary>
        /// <param name="memory"></param>
        /// <param name="ptr"></param>
        public MemoryObject(Memory memory, MemoryPtr ptr) {
            this.Memory = memory;
            this.MemoryPtr = ptr;
            // 设置数据指针
            keyAddr = (long*)(ptr.IntPtr + 1);
            valueAddr = (long*)(ptr.IntPtr + 9);
            this.Keys = new MemoryList(memory, GetKeys());
            this.Values = new MemoryList(memory, GetValues());
        }

        /// <summary>
        /// 获取对象键集合列表
        /// </summary>
        /// <returns></returns>
        public MemoryPtr GetKeys() {
            // 定义返回指针描述
            MemoryPtr res = new MemoryPtr() {
                Type = MemoryTypes.List,
                Size = Memory.List_Size,
            };
            // 设置数据指针
            res.IntPtr = new IntPtr(*keyAddr);
            // 返回结果
            return res;
        }

        /// <summary>
        /// 获取对象值集合列表
        /// </summary>
        /// <returns></returns>
        public MemoryPtr GetValues() {
            // 定义返回指针描述
            MemoryPtr res = new MemoryPtr() {
                Type = MemoryTypes.List,
                Size = Memory.List_Size,
            };
            // 设置数据指针
            res.IntPtr = new IntPtr(*valueAddr);
            // 返回结果
            return res;
        }

        /// <summary>
        /// 添加一个对象的属性，然后获取值的指针
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public MemoryPtr AddKey(string key) {
            // 添加键
            this.Keys.AddItem(this.Memory.CreateString(key));
            // 添加并返回值
            return this.Values.AddItem();
        }

        /// <summary>
        /// 添加一个对象的属性，然后获取值的指针
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public MemoryPtr GetKey(string key) {
            // 设置列表项目
            int idx = this.Keys.GetIndex(key);
            if (idx < 0) throw new Exception($"未在对象中找到名称为'{key}'子对象");
            return this.Values.GetItem(idx);
        }

        /// <summary>
        /// 添加一个对象的属性，然后获取值的指针
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public MemoryPtr GetKeyValue(string key) {
            // 设置列表项目
            int idx = this.Keys.GetIndex(key);
            if (idx < 0) throw new Exception($"未在对象中找到名称为'{key}'子对象");
            MemoryPtr res= this.Values.GetItemContent(idx);
            return res;
        }

        /// <summary>
        /// 添加一个对象的属性，然后获取值的指针
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public void SetKeyValue(string key, MemoryPtr ptr) {
            // 设置列表项目
            int idx = this.Keys.GetIndex(key);
            if (idx < 0) throw new Exception($"未在对象中找到名称为'{key}'子对象");
            this.Values.SetItemContent(idx, ptr);
        }

        /// <summary>
        /// 添加一个对象的属性，然后获取值的指针
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public void SetKeyValue(string key, IntPtr ptr) {
            // 设置列表项目
            int idx = this.Keys.GetIndex(key);
            if (idx < 0) throw new Exception($"未在对象中找到名称为'{key}'子对象");
            this.Values.SetItemContent(idx, ptr);
        }

        /// <summary>
        /// 判断键是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key) {
            // 返回结果
            return this.Keys.GetIndex(key) >= 0;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose() {
            //throw new NotImplementedException();
        }
    }
}
