---
title: Allocations in F#
date: 2022-12-01
draft: true
tag: fsharp, performance
---

Equality and Comparison are two critical components of how a programming language works. There are a variety of approaches to the problem, and the implications can lead to massive differences in performance. By the end of this article, you will understand how the default equality and comparison of the F# Record and Discriminated Unions behave and the performance implications.

## Values vs. References

Equality is a surprisingly deep subject in Computer Science. I come from an Operations Research background, so I have a more math-based concept of equality. For me, I thought that if you have a set `A` and a set `B` that contain the same values, they should be equal, right? Well, the answer is...maybe 😉.

To understand equality, we must first talk about types. In dotnet, we have two broad sets of types: Value and Reference. The important distinction between these types is how the CLR treats their memory[^1].

A Reference type will have its memory allocated on the Heap. An array is an example of a reference type for the CLR. When you pass an array to a method, you are not passing the array itself but a reference to where the array's memory is in the heap.

A Value type, on the other hand, is not necessarily allocated on the Heap. Some people may say that it is allocated on the stack. This is not always the case. A Value type could be allocated on the Stack, or it could be created inside the memory of a Reference type and exist on the Heap. An `int` is a Value type. You could create an `int` on the stack by binding it to a value like so:

```fsharp
let x = 1
```

If you were to draw a picture of what the memory in the computer looks like, it would be this.

<img src="D:\Documents\GitHub\blog\content\blog\2022\10\fsharp-equality-and-comparison\image-20221011152105140.png" alt="image-20221011152105140" style="zoom: 33%;" />

 `x` would be in the current stack frame, and the value in that address would be `1`. In another case, you could be allocating an array of `int`. The array is a Reference type, but the elements inside of the array are a Value type. Here we declare an array `test` and fill it with some `int` values.

```fsharp
let test = [|1; 2; 3; 4|]
```

Again, if we draw a picture of the memory of our application, it would look like this.

<img src="D:\Documents\GitHub\blog\content\blog\2022\10\fsharp-equality-and-comparison\image-20221011153723727.png" alt="image-20221011153723727" style="zoom:33%;" />

The `test` array is a reference type whose memory exists on the heap. The `int` elements inside of `test` are a Value type. This means when we access these elements, instead of getting a reference to the element, we get a physical copy. Let's get the first element from the `test` array and see what our memory looks like now.

```fsharp
let y = test[0]
```

<img src="D:\Documents\GitHub\blog\content\blog\2022\10\fsharp-equality-and-comparison\image-20221011153816032.png" alt="image-20221011153816032" style="zoom:33%;" />

Notice that `y` has a value on the stack. It does not point anywhere in the Heap. If we were to change the value of `y` we would not be changing the elements in the `test` array since `int` is a value type[^2].

If we were to bind another value to the `test` reference, it would not copy the elements of the `test` array, it would just create another Reference. Let's create another array, `test2`, and have it bound to `test`.

```fsharp
let test2 = test
```

Now let's look at what our memory looks like.

<img src="D:\Documents\GitHub\blog\content\blog\2022\10\fsharp-equality-and-comparison\image-20221011154039675.png" alt="image-20221011154039675" style="zoom:33%;" />

`test2` is another reference pointing to the same location in the Heap. Now for the 

## What is Equality?




[^1]: Technically, there is more that differentiates these two broad categories of type,s but the point of this article is equality and comparison.
[^2]: There are ways to return a reference to an element in a collect,ion but that's a more advanced scenario than what we want to cover.
