using System;
using System.Collections.Generic;

namespace textHandlerTest
{
    public class TestProgress<T> : IProgress<T>
    {
        public List<T> Reports { get; } = new List<T>();

        public void Report(T value)
        {
            Reports.Add(value);
        }
    }
}

