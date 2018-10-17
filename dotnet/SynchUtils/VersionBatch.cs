using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynchUtils {
    public  class VersionBatch {
        private int current;
        private int size;

        public VersionBatch() {
            current = 0;
            size = 0;
        }

        public int Add() {
            size++;
            return current;
        }

        public void Remove(int current) {
            if (this.current != current || size == 0)
                throw new InvalidOperationException();
            size--;
        }

        public int Current  {
            get {
                return current;
            } 
        }

        public void NewBatch() {
            current++;
            size = 0;
        }
    }
}
