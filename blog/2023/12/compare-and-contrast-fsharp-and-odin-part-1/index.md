# 

# Compare and Contrast F# and Odin - Part 1

I have recently been talking online about the need to improve the importance of my .NET code. I have successfully created high-performance solutions using purely F# and will continue to. There comes a point, though, when you are fighting the system you are building on so much that you end up looking for another tool.

I have long been a programming language geek and have enjoyed learning about other languages and the problems that motivated them. Early in my career, I prized productivity more than anything else, and to be honest, I still do. What makes a language productive is how well it fits your problem space. F# is a high-productivity language for many problem domains and will remain in my toolbox for a long time. I have reached the point where I need to push the performance of the product I am building, and the .NET platform doesn't fit anymore.

While exploring other languages, I landed on [Odin](https://odin-lang.org/), a language developer by Bill Hall (aka Ginger Bill). Odin appears to be a language purpose-built for the domain that I am working in. As I have started diving into Odin code, I thought it would be valuable to provide a compare and contrast of the two languages. Spoiler: neither language is superior to the other. To judge a language fairly, you must consider the problem space it is trying to address. F# and Odin have different goals, which I want to highlight.

Fortunately, the .NET runtime and F# make it easy to call Odin from F# so a solution doesn't have to be completely one or the other. The current project I am working on has an F# shell and an Odin core. We leverage both languages for what they are best at.

## The Goal of F#

From my understanding, the goal of F# was to provide a high-productivity language in the ML family for .NET. If you want to know the history, check out the [HOPL paper](). It gives a detailed history of how the language came to be and the forces that shaped it. I think of F# as a language designed to get work done; it just happens to have a functional flare to it. It leans toward immutability and composing solutions by defining types and functions that operate on those types. In 99% of scenarios, you can achieve excellent performance with F#. It wants to simplify developers’ lives so it has safe and robust defaults. Being built on top of .NET it has Garbage Collection, which is a boon to developers who don't want to care about managing memory (more on this later).

F# has a terse syntax that provides minimal noise and gets out of your way. It also has access to the enormous .NET ecosystem which can help bootstrap any project. I still believe F# is an excellent first language, and if I need to "just get something done," F# is still my preferred tool. It has excellent tooling, and debugging is straightforward due to the language’s design dragging you down the pit of success.

## The Goal of Odin

Odin started in July 2016, born out of Bill Hall’s frustration with the C and C++ languages. I recommend [this video](https://youtu.be/rCqFdYUnC24?si=XdL0uAsqvWDriszi) for a quick introduction to the language. Bill's background is in Physics, and he has many years of programming experience. He's currently employed by [JangaFX](https://jangafx.com/), which develops real-time simulation engines for movies and video games. I won't put words in Bill's mouth, so the following is my synopsis of Odin.

I see Odin as one of the C-Next languages. It is very purposefully not intended as a successor to C++. I think this is an important distinction. Many of the other languages I have looked at appeared to be more interested in becoming the next C++. I would put Rust, Jai, Nim, and Vale in this category. Odin is conservative in its feature set; it just does it very well. This may not seem impressive, but there is something incredibly pleasing about using an extremely well-made tool.

Odin is intended as a language for high-performance computing. You can see that in its extensive support for memory allocators and controlling memory layout. Including the Struct of Array layout using `#soa` and the `using` keyword makes it easy to play with how your data is laid out and accessed.

Odin intends to make writing high-performance code joyful, and I can tell you it succeeds wildly after working in it for a couple of weeks.

Let's now start comparing these languages and discussing their differences.

## Defining Types

### Records and Structs

Both F# and Odin make it easy to define types. The most significant difference is that F# defaults to creating objects on the heap, while Odin defaults to the stack. Let's define a `Chicken` type in both languages. The default for F# would be to define `Chicken` as a Record, an immutable and heterogenous collection of fields allocated on the Heap.

F#
```fsharp
type Chicken = {
    Name: string
    Age: int
    Size: float
}
```

Odin doesn't have the idea of a Record. Instead, the closest thing we have is a `struct` which is allocated on the Stack.

Odin
```odin
Chicken :: struct {
    Name: string,
    Age: i32,
    Size: f64,
}
```

You see that it is nearly the same. To define `Chicken` as a struct in F#, you need to add the `[<Struct>]` attribute.

F#

```fsharp
[<Struct>]
type Chicken = {
    Name: string
    Age: int
    Size: float
}
```

Note that the F# `Chicken` is immutable. You cannot change the values of the fields. If you want to do that, you need to add the `mutable` keyword.

F#

```fsharp
[<Struct>]
type Chicken = {
    mutable Name: string
    mutable Age: int
    mutable Size: float
}
```

This is unusual in F#. Generally, if you wanted an updated version of a Record, you would use the `with` syntax to update the appropriate fields. Here's an example of creating a new instance of `Chicken` with a new name value of "Drumstick".

F#

```fsharp
let c1 = { Name = "Clucky"; Age = 1; Size = 2.0 }
let newChicken = { c1 with Name = "Drumstick" }
```

If you add the `mutable` keyword to each of the fields of `Chicken` you now have something equivalent to the Odin `Chicken`.

Let's compare the syntax for creating an instance of `Chicken` in F# and Odin.

F#
```fsharp
let c = { Name = "Clucky"; Age = 1; Size = 2.0 }
```

Odin
```odin
c := Chicken{ "Clucky", 1, 2.0, }
```

You will see in this case that the F# is a little more verbose. This is because Odin allows you to create instances of `struct`s by providing arguments in the order they are declared. If you wanted to be more explicit, you could use the field names as well:

Odin

```odin
c := { Name = "Clucky", Age = 1, Size = 2.0, }
```

If we use the default `let` binding in F#, we still won't have quite the same behavior as Odin. Though the fields of the F# `Chicken` type are mutable, we aren't allowed to change them without taking a mutable binding. Let's do the Odin equivalent in F#.

F#
```fsharp
let mutable c = { Name = "Clucky"; Age = 1; Size = 2.0 }
```

We now have something that behaves exactly like the Odin `Chicken`. Now, is the Odin version of the F# version better? Before you even think about that question, you must ask, “Under what criteria are we judging these languages?" In a later post, I will dive more into this. For now, I want you to observe what F# makes easy and what Odin makes easy. That will inform the design intention behind these languages and, therefore, how we should judge them.

I showed you how to express the F# `Chicken` with the same functionality as the Odin `Chicken`. Notice that I did not do the inverse. From my limited knowledge, it's impossible to express the idea of immutable types in Odin, which makes sense given the roots of the language. Let's move on to Discriminated Unions.

### Discriminated Unions

Both F# and Odin have algebraic type systems, meaning they have “AND” and "OR" types. An F# Record or Odin `struct` are AND types. You can think of a `Chicken` as being a "Name" **and** an "Age" **and** a "Size". An OR type is one where you have a type which could be a Foo **or** a Bar **or** a Baz. Let's say we wanted to define a type `Value` which could be an `int`, `float`, or `string`. In F#, we would use a Discriminated Union (DU) like so:

F#

```fsharp
type Value =
    | A of int
    | B of float
    | C of string
```

An F# DU requires that each case of the DU have a label and a type associated with the label. The labels in this example are "A", "B", and "C". The value associated with "A" is an `int`, the value associated with "B" is a `float`, and the value associated with the label "C" is a `string`. Odin's DUs are just called Unions and are similar to F# but drop the Label. Only types make up the cases of a Union in Odin. Here is what `Value` looks like in Odin:

Odin

```odin
Value :: union {
    i32, // Equivalent to the F# `int`
    f64, // Equivalent to the F# `float`
    string
}
```

The lack of labels means that the following F# cannot be expressed in Odin.

F#

```fsharp
type OtherValue =
    | Case1 of int
    | Case2 of int
    | Case3 of int
```

The `OtherValue` type has three possible cases, but the underlying type is always an `int`. This can't be written in Odin since the Union type in Odin only comprises types. This may sound bad, but Odin has another trick: the `distinct` types. Odin is described as a distinctly typed language, which means that just because two types share the exact same underlying representation, they should not be considered equivalent types. What makes this easy is having a `distinct` keyword. `distinct` allows you to define a new type with the same data representation as another type, but the compiler does not consider them interchangeable. Let’s see this in action.

We return to our F# example and say that the different cases correspond to IDs in our domain. Instead of defining our union as `OtherValue`, we are going to say it is a `BirdId` which could be holding the Id for three different kinds of birds: Chicken, Turkey, and Goose.

F#

```fsharp
type BirdId =
    | ChickenId of int
    | TurkeyId of int
    | GooseId of int
```

How do we do something like this in Odin? We use the `distinct` keyword.

Odin

```odin
ChickenId :: distinct i32
TurkeyId :: distinct i32
GooseId :: distinct i32

BirdId :: union {
    ChickenId,
    TurkeyId,
    GooseId,
}
```

Even though all of these IDs use an `int` as the underlying value, we have told Odin that it should consider them distinct. I have found the combination of `distinct` with union to solve any issues with not having labels for the different cases. In F#, you would likely use a single-case DU to wrap the `int` values to differentiate them.

## Next Time

In the next post, we’ll discuss functions and procedures and how F# and Odin differ. I hope by the end, you’ll have a deeper appreciation for both languages and understand how their different design goals formed their evolution. You’ll understand their strengths and weaknesses and see how being able to use both of them can lead to incredibly powerful solutions.

