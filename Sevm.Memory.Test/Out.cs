using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sevm.MemoryTest {
    internal static class Out {

        public static string GetPtrString(MemoryPtr ptr) {
           return $"{{Type:\"{ptr.Type.ToString()}\", Size:{ptr.Size}, IntPtr:0x{ptr.IntPtr.ToString("x")}}}";
        }

    }
}
