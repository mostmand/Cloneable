# Cloneable
Auto generate Clone method using C# Source Generator

There are times you want to make a clone of an object. You can implement a clone method, but when a developer adds a new Field or Property the clone method should be changed too. Another way is to use reflection which is not performant. 
This source generator saves your time by generating the boilerplate code for cloning an object.

## Usage

You can add clone method to a class by making it partial and adding the attribute `Cloneable` on top of it. An example is provided in Cloneable.Sample project.

Source generators are introduced in dotnet 5.0. So make sure to have Visual Studio 16.8 or dotnet 5.0 sdk installed.

Here is an example:

```csharp
[Cloneable]
public partial class Foo
{
    public string A { get; set; }
    public int B { get; set; }
}
```
