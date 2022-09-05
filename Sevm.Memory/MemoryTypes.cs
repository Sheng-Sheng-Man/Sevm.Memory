using System;
using System.Collections.Generic;
using System.Text;

namespace Sevm {

    /// <summary>
    /// 虚拟内存指针类型
    /// </summary>
    public enum MemoryTypes {

        /// <summary>
        /// 空类型
        /// </summary>
        None = 0x00,

        /// <summary>
        /// 字节类型
        /// </summary>
        Byte = 0x01,

        /// <summary>
        /// 整型
        /// </summary>
        Integer = 0x02,

        /// <summary>
        /// 长整型
        /// </summary>
        Long = 0x03,

        /// <summary>
        /// 单精度类型
        /// </summary>
        Float = 0x04,

        /// <summary>
        /// 双精度类型
        /// </summary>
        Double = 0x05,

        /// <summary>
        /// 字符串类型
        /// </summary>
        String = 0x06,

        /// <summary>
        /// 列表类型
        /// </summary>
        List = 0x11,

        /// <summary>
        /// 列表类型
        /// </summary>
        ListItem = 0x12,

        /// <summary>
        /// 对象类型
        /// </summary>
        Object = 0x13,

        /// <summary>
        /// 函数类型
        /// </summary>
        Function = 0x14,

        /// <summary>
        /// 原生对象
        /// </summary>
        NativeObject = 0x21,

        /// <summary>
        /// 原生函数
        /// </summary>
        NativeFunction = 0x22,

        /// <summary>
        /// 数值
        /// </summary>
        Value = 0x99,


    }
}
