using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TweakersUserBadge.Helpers {
    public class AsyncLazy<T> : Lazy<Task<T>> {
        public AsyncLazy(Func<T> valueFunc) : base(() => Task.Factory.StartNew(valueFunc)) { }
        public AsyncLazy(Func<Task<T>> taskFunc) : base(() => Task.Factory.StartNew(taskFunc).Unwrap()) { }
    }
}
