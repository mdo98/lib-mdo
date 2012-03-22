using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace MDo.Learning.Supervised
{
    [DataContract]
    public class LinearRegression<T> : ISupervisedModel<T>
    {
        public void Load(Stream input)
        {
        }

        public void Save(Stream output)
        {
        }

        public void Train(IEnumerable<T> examples)
        {
        }

        public object Predict(T item)
        {
        }
    }
}
