using System;
using System.Collections.Generic;
using System.Text;

namespace textHandlerTest
{
    public class TestProgress<T> : IProgress<T>
    {
        public List<T> Reports { get; } = new List<T>();
        public List<Exception> Exceptions { get; } = new List<Exception>();

        public void Report(T value)
        {
            try
            {
                Reports.Add(value);
                OnReport?.Invoke(value);
            }
            catch (Exception ex)
            {
                Exceptions.Add(ex);
            }
        }

        public event Action<T> OnReport;
    }
}
