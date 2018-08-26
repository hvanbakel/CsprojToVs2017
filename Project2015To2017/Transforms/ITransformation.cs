using System;
using Microsoft.Extensions.Logging;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	public interface ITransformation
    {
		/// <summary>
		/// Alter the provided project in some way
		/// </summary>
		/// <param name="definition"></param>
        void Transform(Project definition);
    }
}
