using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MDo.Interop.R.Core;

namespace MDo.Interop.R.Models.Test
{
    public class Interop_R_Models_RandomForestModel : Interop_R_Models_TreeModel
    {
        public override TreeModel GetModel(RVector observed_X, RVector observed_Y)
        {
            return RandomForestModel.LinearModelForClassification(observed_X, observed_Y);
        }

        public override bool[] GetPredictionErrors(RVector y_actual, RVector y_Fit)
        {
            int numObserved = y_actual.NumRows;
            bool[] y_errors = new bool[numObserved];

            IList<string> classes = new List<string>(new SortedSet<string>() { false.ToString(), true.ToString() });
            int falseClass = classes.IndexOf(false.ToString()) + 1,
                trueClass  = classes.IndexOf(true.ToString())  + 1; // 1-based

            for (int i = 0; i < numObserved; i++)
            {
                y_errors[i] = ((bool)y_actual.Values[i,0]
                    ? ((int)y_Fit.Values[i,0] != trueClass)
                    : ((int)y_Fit.Values[i,0] != falseClass));
            }
            return y_errors;
        }

        public override void PrintData(RVector x, RVector y_actual, RVector y_Fit, bool[] test_errors)
        {
            int numFeatures = x.NumCols, numObserved = x.NumRows;
            for (int j = 0; j < numFeatures; j++)
            {
                Console.Write(string.Format("X{0}\t", j));
            }
            Console.WriteLine("Y_actual\tY_Fit\tERROR");

            SortedSet<string> sortedClasses = new SortedSet<string>();
            foreach (bool cls in new bool[] { true, false })
            {
                sortedClasses.Add(cls.ToString());
            }
            IList<string> classes = new List<string>(sortedClasses);
            IDictionary<int, string> classesDict = new Dictionary<int, string>();
            foreach (bool cls in new bool[] { true, false })
            {
                string clsAsString = cls.ToString();
                classesDict.Add(classes.IndexOf(clsAsString) + 1 /* 1-based */, clsAsString);
            }

            for (int i = 0; i < numObserved; i++)
            {
                for (int j = 0; j < numFeatures; j++)
                {
                    Console.Write(string.Format("{0:F3}\t", x.Values[i,j]));
                }
                Console.WriteLine("{0}\t{1}\t{2}", y_actual.Values[i,0], classesDict[(int)y_Fit.Values[i,0]], test_errors[i] ? "X" : string.Empty);
            }
        }
    }
}
