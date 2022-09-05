// See https://aka.ms/new-console-template for more information
using Sevm;
using Sevm.MemoryTest;

Console.WriteLine("Hello, World!");

int tick1 = 0;
int tick2 = 0;

unsafe {
    using (Sevm.Memory mem = new Sevm.Memory()) {
        List<Sevm.MemoryPtr> ptrs = new List<Sevm.MemoryPtr>();
        tick1 = Environment.TickCount;
        var i = mem.CreateDouble(0);
        Console.WriteLine($"i:{Out.GetPtrString(i)}");
        Console.WriteLine($"Offset:{mem.Offset}");
        //double i = 0;
        //int sum = ptrs.AddNumber();
        var sum = mem.CreateDouble(0);
        Console.WriteLine($"sum:{Out.GetPtrString(sum)}");
        Console.WriteLine($"Offset:{mem.Offset}");

        var ls = mem.CreateList().GetList(mem);
        Console.WriteLine($"ls:{Out.GetPtrString(ls.MemoryPtr)}");
        Console.WriteLine($"Offset:{mem.Offset}");

        ls.AddItem(i);
        Console.WriteLine($"ls[0]:{Out.GetPtrString(ls.GetItem(0))}");
        Console.WriteLine($"Offset:{mem.Offset}");

        ls.AddItem(sum);
        Console.WriteLine($"ls[1]:{Out.GetPtrString(ls.GetItem(1))}");
        Console.WriteLine($"Offset:{mem.Offset}");

        //double* sum = (double*)ptrs[idx2].IntPtr;
        while (*ls.GetItemContent(0).DoublePtr <= 10000) {
            *ls.GetItemContent(1).DoublePtr += *ls.GetItemContent(0).DoublePtr;
            //mem.DoubleAdd(i, 0.01);
            *ls.GetItemContent(0).DoublePtr += 0.01;
        }
        Console.WriteLine(*i.DoublePtr);
        Console.WriteLine(*sum.DoublePtr);

        //Console.WriteLine($"Count:{mem.GetListCount(ptrs[ls])}");
        //MemoryPtr list1 = mem.GetListItemContent(ptrs[ls], 1);
        //Console.WriteLine($"list[1]:{Out.GetPtrString(list1)}");
        //MemoryPtr list1Content = mem.GetListItemContent(list1);
        //Console.WriteLine(*list1.DoublePtr);

        tick2 = Environment.TickCount;
        Console.WriteLine($"{tick2 - tick1}ms");

        Console.WriteLine($"{ls.ConvertListToString()}");

        // 对象测试
        var obj = mem.CreateObject().GetObject(mem);
        obj.AddKey("name");
        obj.SetKeyValue("name", mem.CreateString("lucky"));
        obj.AddKey("age");
        obj.SetKeyValue("age", mem.CreateDouble(24));
        obj.AddKey("sex");
        obj.SetKeyValue("sex", mem.CreateString("女"));
        Console.WriteLine($"name.index:{obj.Keys.GetIndex("name")}");
        Console.WriteLine($"name:\"{obj.GetKeyValue("name").GetString()}\"");
        Console.WriteLine($"age.index:{obj.Keys.GetIndex("age")}");
        // 输出占用空间
        Console.WriteLine(mem.SpaceOccupied - mem.Size + mem.Offset);
        Console.WriteLine($"age:{obj.GetKeyValue("age").GetDouble()}");


    }
}

Console.ReadKey();
