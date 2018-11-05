using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aula_2018_10_30 {
    public class Lazy<T> {
        private Func<T> supplier;

        private object monitor;
        private volatile bool hasValue;
        private T value;

        public Lazy(Func<T> supplier) {
            this.supplier = supplier;
            monitor = new object();
        }

        private T Value {
            get {
                if (!hasValue) {
                    lock (monitor) {
                        if (!hasValue) {
                            value = supplier();
                            hasValue = true;
                        }
                    }  
                }
                return value;
            }
        }

    }
}
