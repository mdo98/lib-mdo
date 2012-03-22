using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MDo.Learning
{
    public interface IModel<T>
    {
        void Train(IEnumerable<T> examples);
    }

    public interface ISupervisedModel<T> : IModel<T>
    {
        object Predict(T item);
    }
}
