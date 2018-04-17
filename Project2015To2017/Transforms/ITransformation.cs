using System;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	internal interface ITransformation
    {
		/// <summary>
		/// Alter the provided project in some way
		/// </summary>
		/// <param name="definition"></param>
		/// <param name="progress"></param>
        void Transform(Project definition, IProgress<string> progress);
    }
}
