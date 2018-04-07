using System;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	internal interface ITransformation
    {
        Project Transform(Project definition, IProgress<string> progress);
    }
}
