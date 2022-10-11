# The Under Appreciated Power of Object Expressions


There is a feature of F# this is incredibly powerful and rarely talked about: The Object Expression. It is such a simple idea that it is almost boring but the implications of it are profound. We often define an interface to abstract the implementation of an object so that we can code against different implementations. By programming against an `IEnumerable` or `IDictionary`, we can write algorithms which work against a host of different backing data structures. To use these abstractions, we implement the interface on our classes.

What if I told you that there is a different way? What if I told you it was possible to build objects in a Frankenstien style where we could define each member as we chose? This may sound a little odd but the freedom it gives you in testing is incredible. Before we dive deep into how we use Object Expressions, let's talk about what they are.

## Object Expression: What art thou?

Whenever I start working with a new developer, there are two things I stress with them when it comes to reasoning about F#.

1. F# does not have variables, it has values$^1$
2. Everything is an expression. Everything returns something, even if that something is nothing$^2$

These two ideas can cause some cognitive dissonance for imperative programmers used to C, C++, C# or their brethren. Those are great languages. The family of C languages and their imperative programming cousins just have a different philosophy. In imperitive programming you can just perform an action without it returning anything. In F#, something will always be returned because everything is an expression.

Alright, so I've established that F# has a different way of thinking. But what makes Object Expressions special? Well, if we were working in C# and we wanted to create an object which fulfilled an interface contract, we would need to declare that type and then implement the interface. Let's declare an incredibly simple interface `IAnimal`.

```csharp
interface IAnimal
{
    string Name
    {
        get;
    }
    double Size
    {
        get;
    }
}
```

We have now declared an `IAnimal` interface. If we want to work with an instance of it, we must define a class which implements this interface. Let us define a `Chicken` class which implements this interface. Let's assume that I want instances of this class to be immutable to protect against mutation. I'll go ahead and make my life easier by using a C# 9.0 feature, Records.

```csharp
public record Chicken : IAnimal
{
    public string Name { get; }
    public double Size { get; }
    public Chicken(string name, double size) => (Name, Size) = (name, size);
}
```

Okay, I have an interface and now I have a class which implements that interface. If I want to play around with functions or methods that take that interface as an argument, I will need to create instances of `Chicken` unless I want to declare another class which implements `IAnimal`. For testing purposes I may define a `MockAnimal` which has different behaviors. I may have to define several different `MockAnimal` classes if I want test different behaviors.

So, this is all fine and doesn't seem too cumbersome but the more complex the interface the more you will find yourself creating mock versions to test with. F# provides a different mechanism for implementing interfaces, the Object Expression! Let's say we've abstracted the interaction with a database behind an `IRepository` inteface. Let's declare a silly `IRepository` for storing and retrieving Pigs. Here's that interface in F#.

```fsharp
type Pig = {
    // Some fields which define a pig
}

type IRepository =
    abstract member GetById : id:int64 -> Pig
    abstract member Save : pig:Pig -> unit
```

Our interface has two methods. One for saving Pigs and one for retrieving Pigs by an Id. In both cases we could possibly get an exception due to the database not being available. If we were in C# and we wanted to work with this interface, we would need to define a class which implements this interface. What if we could just define an object which implements this interface out of thin air? Let me show you how to do that.

```fsharp
let geyById id = // A function in our domain that does the work of querying

let save (pig:Pig) = // A function in our domain that does the work of saving

let repository =
    { new IRepository with
        member _.GetById id = getById id
        member _.Save pig = save pig
    }
```

What just happened? There are two functions in the domain which know how to do the work of retrieving and saving our pigs: `getById` and `save`. Instead of defining a new class, we said, "Hey F#, I want an object which fulfilles the `IRepository` interface and I want you to use these functions to do it." We don't need to define a class; we can just make an object and bolt together functions to fulfill the `IRepository` contract. I think of this as interface by Frankenstein assembly.

## Buy Why Do I Care?

While that example may be cute, it may not wow you. Let's talk where this is impactful: testing! One of the most difficult challenges we face when writing robust code is ensuring that our services both succeed when given clean inputs but also fail, the way we want them to. As a policy, we always write tests which confirm that the happy path works as intended and that the various failure modes are dealt with correctly.

This type of testing gets painful when you have external dependencies like databases or APIs that are outside your domain. You either must be able to stand up a mock version of the service in your CI/CD pipeline or implement mock versions of your classes just for testing. What if instead of that, we could create an object which behaved exactly as intended?

Let's look at testing the `IRepository` interface. I have abstracted out what the backing service is in the case. Let's I want to verify that my business logic responds appropriately to a `SqlException`. Rather than standing up a SQL Server instance and artificially creating a bad state, I'll create an object that behaves exactly how I want it.

```fsharp
let mockRepository =
    { new IRepository with
        member _.GetById id = raise (new SqlException())
        member _.Save pig = ()
    }
```

This `mockRespository` will always raise a `SqlException` and will appear to be successful any time I call the `Save` method. I can have my mock repository behave any way that I want by just swapping out different functions to give the desired behavior. I don't have to define a new class each time I want slightly different behavior. Where this gets even more exciting is when you are trying to test failure modes which involve multiple external services. It is relatively easy to abstract all of them behind interfaces and the construct mock instances that will behave exactly as intended.

I believe that the Object Expression feature in F# is really under appreciated. It makes it easy to create arbitrary implementations of interfaces. I am sure that it has utility outside of testing as well. I have found that it makes unit and property-based testing relatively painless. I hope this opens your eyes a little to how you can streamline your testing needs in F#.

>$^1$ Technically it is possible to declare a variable in F#. You must add the `mutable` modifier to the declaration. The key point is that F# deals with values by default, not variables.

>$^2$ When F# returns "nothing", it returns the type `unit`. The key thing is that even an action which does not "seem" to return a value is still actually returning something. The implications are deeper than I have time for here. I encourage the curious to look into ["Category Theory for Programmers"](https://bartoszmilewski.com/2014/10/28/category-theory-for-programmers-the-preface/) by Barstosz Milewski.

