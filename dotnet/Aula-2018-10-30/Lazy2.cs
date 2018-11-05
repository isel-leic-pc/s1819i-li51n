using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Aula_2018_10_30 {
    public class Lazy2<T> {
        private Func<T> supplier;

        private class Boxed {
            internal T value;
            internal Boxed(T value) {
                this.value = value;
            }
        }
        private Boxed boxedValue;
        
        public Lazy2(Func<T> supplier) {
            this.supplier = supplier;
        }

        private T Value {
            get {
                if (boxedValue == null) {
                    Interlocked.CompareExchange(
                        ref boxedValue, new Boxed(supplier()), null);
                }
                return boxedValue.value;
            }
        }

    }
}
