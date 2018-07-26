# upstream links
To support advanced and always precise Condition attribute parsing we use parts of MSBuild code licensed under MIT. This document is designated to assist in updating imported code or alike purposes.

## Code version
The code was brought in on July 21st 2018. The latest commit ID for Conditionals\ directory was *f147a76*.

## ConditionEvaluator
Has parts of [ConditionEvaluator](https://github.com/Microsoft/msbuild/blob/master/src/Build/Evaluation/ConditionEvaluator.cs) code in "MSBuild Conditional routine" region.

Changes:
* Some method doc changes (usage section was incorrect)
* SinglePropertyRegex was wrapped in Lazy<T>
* IConditionEvaluationState was moved out to the outer scope and then to Conditionals\ directory

## Conditionals\ directory
Most of it is based on [Microsoft.Build.Evaluation\Conditionals](https://github.com/Microsoft/msbuild/tree/master/src/Build/Evaluation/Conditionals), with some parts from [Microsoft.Build.Shared](https://github.com/Microsoft/msbuild/tree/master/src/Shared).

Changes:
* Removed some deprecated code for compat with old MSBuild expression parser (too many dependencies)
* Removed many verify-guards so that if in doubt an exception will likely be thrown (we don't need user-oriented error reporting facilities if conditionals contain syntax errors)
* Included some utility classes (CharacterUtilities, ConversionUtilities, ErrorUtilities)
* Changed namespace to match new location