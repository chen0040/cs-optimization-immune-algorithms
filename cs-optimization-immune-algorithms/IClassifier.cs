﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AIA
{
    public interface IClassifier<T>
    {
        void Train(IEnumerable<T> data_store);

        string Predict(T data_set);
    }
}
