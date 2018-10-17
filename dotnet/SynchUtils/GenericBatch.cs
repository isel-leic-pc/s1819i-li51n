using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynchUtils {
    public class GenericBatch<T> {

        public class Request {
            public T value;

            public Request(T val) {
                value = val;
            }
        }

        private Request current;

        private int size;

        public GenericBatch(T initial) {
            current = new Request(initial);
            size = 0;
        }

        public Request Add() {
            size++;
            return current;
        }

        public void Remove(Request current) {
            if (this.current != current || size == 0)
                throw new InvalidOperationException();
            size--;
        }

        public Request Current {
            get {
                return current;
            }
        }
        public void NewBatch(T state) {
            current = new Request(state);
            size = 0;
        }
    }
}
