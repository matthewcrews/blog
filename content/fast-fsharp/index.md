---
title: 'Fast F#'
description: 'Resources for writing Fast F#'
intro_image: "images/packing-bay.jpeg"
intro_image_absolute: true
intro_image_hide_on_mobile: false
---

 F# is a beautiful language that is concise, expressive, and efficient. I'm obsessed with making it run as fast as possible. On this page, you will find the resources I find most helpful in learning to write high-performance code. I have arranged it by content type and provided a summary of my thoughts so you can find what will be most helpful for you. If you have any suggestions, please email me at matthew@crews.email or message me on Twitter [@McCrews](https://twitter.com/McCrews).

## Books

I have found all of these books incredibly helpful and have them in my library. The heading is a link to the book's webpage or a link where you can purchase them. I am not an affiliate of any website, so I make no money on purchases.

### The Essentials

These first four are what I would consider the essentials for any .NET developer serious about writing high-performance code.

#### [Pro .NET Memory Management](https://prodotnetmemory.com/)

"Pro .NET Memory Management" is a tome detailing how the CLR manages memory. Much of .NET performance comes down to memory access patterns and understanding how garbage collection works in .NET. [Konrad Kokosa](https://twitter.com/konradkokosa) has done an incredible job documenting how the CLR manages memory and what you can do to maximize performance.

#### [Pro .NET Benchmarking](https://aakinshin.net/prodotnetbenchmarking/)

"Pro .NET Benchmarking" is essential for understanding how to measure your code's performance accurately. Many gotchas can lead you to draw incorrect conclusions on how your code will perform in real-world scenarios. [Andrey Akinshin](https://aakinshin.net/about/) does an excellent job walking you through how to create good benchmarks that will allow you to see how well your code will perform.

#### [Writing High-Performance .NET](https://www.writinghighperf.net/)

[Writing High-Performance .NET](https://www.writinghighperf.net/) by [Ben Watson](https://twitter.com/benmwatson), provides excellent advice on patterns and practices to get the most performance out of .NET. The book offers many practical examples of what you can do to write code that will run fast on the CLR. I re-read regularly and each time I learn more.

#### [The Algorithm Design Manual](https://www.amazon.com/Algorithm-Design-Manual-Computer-Science/dp/3030542556/)

This book is not .NET specific but has been so helpful for me that I believe it belongs in the Essentials list. [The Algorithm Design Manual](https://www.amazon.com/Algorithm-Design-Manual-Computer-Science/dp/3030542556/) by [Steven Skiena](https://www3.cs.stonybrook.edu/~skiena/) is my first stop when I'm confronted with a new problem. It does an incredible job of talking through real-world problems and then mapping them to the algorithm that best solves that problem. Possibly the best part of the book is the extensive citations. You can quickly find the literature that deals with your specific situation. I recommend reading through it even if you don't have a current problem you are working on because it may give you insight into how to tackle issues you've dealt with in the past.

### Extra Credit

The next set of books is not .NET specific, but I think it provides significant value for those serious about writing fast code.


#### [Data-Oriented Design](https://www.amazon.com/dp/1916478700)

There is a new paradigm developing in the software community called Data-Oriented Programming. We need to think about how our data is laid out and accessed to make our code fast. Even the best algorithms fall apart when the data layout is poorly designed. Therefore, data and how it is arranged should be of primary importance. In his book, Data-Oriented Design, [Richard Fabian](https://twitter.com/raspofabs) does an excellent job laying out the principles of this new paradigm and discussing practical implications.

#### [Game Programming Patterns](https://gameprogrammingpatterns.com/)

[Bob Nystrom](https://twitter.com/munificentbob) is possibly one of the funniest tech authors I have come across. His book "Game Programming Patterns" is both hilarious and immensely practical. Games have long had some of the most demanding performance requirements, and Bob has done an excellent job cataloging the most useful patterns. I enjoy that he starts with a problem and then unpacks the pattern that addresses it. It is an approachable style and makes the need for the pattern clear. At the very least, you should read this for the humor.

#### [The Art of Writing Efficient Programs](https://www.packtpub.com/product/the-art-of-writing-efficient-programs/9781800208117)

This book is targeted to a C++ developer crowd, but there is plenty in here that any dev would find helpful. Fedor Pikus provides excellent insight into how to write programs that will most efficiently run on today's CPUs. About 1/3 of this book is about concurrent C++, which does not apply to me, but the rest of the content was good enough that I thought it bore mentioning. I especially like the section on branchless programming, for which there are not many resources.

#### [Game Engine Architecture](https://www.gameenginebook.com/)

This book is an absolute monster at just over 1,200 pages. I have not read through all of it, but I found what I read helpful. [Jason Gregory](https://twitter.com/jqgregory) has led game engine development on several of the top AAA games in the last decade, so he has some excellent insight into designing large software projects that must perform at the highest level. I recommend this for someone who needs to architect an extensive system or program that needs ultimate performance. I think of this as less of a low-level optimization book and more of a guide to creating a more extensive program.

#### [Modern Computer Architecture and Organization](https://www.packtpub.com/product/modern-computer-architecture-and-organization/9781838984397)

I want to understand how computers work at the lowest level, and this book has been an excellent guide to how CPUs work from the ground up. It starts with binary logic, builds up to circuits, and from there processors. At some point, everyone serious about high-performance code wants to understand how hardware works. I have found this to be the best resource to go from the absolute fundamentals and build up to the complexity of modern CPUs.

## Videos

These videos run the gamut of languages and technologies, but I have found them all helpful in expanding my understanding of writing fast code. The title is a link to where the video can be found.

#### [CppCon 2014: Mike Acton "Data-Oriented Design and C++" - Mike Acton ](https://www.youtube.com/watch?v=rX0ItVEVjHc&t=3752s)

There isn't enough good I can say about this talk. [Mike Acton](https://twitter.com/mike_acton) reminds us that code is meant to solve problems, and the problem is fundamentally one of data transformations. He's channeling years of frustration with people obsessing over design patterns or class hierarchies. Every time I watch this video, I smile. The comments on this video are gold as well. Mike is not alone in his frustration with the industry.

#### [code::dive conference 2014 - Scott Meyers: Cpu Caches and Why You Care](https://www.youtube.com/watch?v=WDIkqP4JbkE&t=1367s)

Scott Meyers provides an excellent talk detailing CPU caches and why we care about them. Cache access patterns have large implications on performance and Scott makes a perfect case why they should be top of mind when writing performant code.

#### [DConf 2017 Day 2 Keynote: Things that Matter -- Scott Meyers](https://www.youtube.com/watch?v=3WBaY61c9sE&t=1108s)

Another talk by Scott Meyers but vital because he unpacks the phrase "premature optimization is the root of all evil." Scott explains how this quote has been taken out of context, overly applied, and caused undue harm. I like it because it faces this dogma and shows how even the paper from whence it came defies this prevailing wisdom.

#### [Everything (maybe too much) about stackalloc With Kevin Jones | Hampton Roads .NET Users Group](https://youtu.be/ufaIXCygDrI)

There are not many videos that detail `stackalloc,` and when I came across this one, I was pleased. It explains this feature in .NET and some of the gotchas to be concerned about when using it in your code.

#### [CppCon 2018: Fedor Pikus “Design for Performance”](https://youtu.be/m25p3EtBua4)

A great talk by Fedor Pikus about how to design for performance: I enjoy this because he shows several different ways of attacking a problem and how performance can be impacted.

#### [Bartosz Adamczewski — Data-oriented design for business applications](https://youtu.be/D7rKRnICZUY)

An excellent talk by [Bartosz Adamczewski](https://twitter.com/badamczewski01) on how Data-Oriented Design can be applied to business applications. He does a great job of taking a practical problem you would face in any enterprise CRUD app and shows you how Data-Oriented Design can be applied. This helped pivot some of my thinking about how widely applicable the design methodology is.

#### [Branchless Programming in C++ - Fedor Pikus - CppCon 2021](https://youtu.be/g-WPhYREFjk)

Branchless Programming is not something I have dived into much, but I still find it fascinating. Fedor surprises you with how far you can push the concept and what performance gains can be had.

## Workshops and Classes

[.NET Memory Expert](https://dotnetmemoryexpert.com/)

This course can be thought of as the companion to Konrad's book. The approach in teaching is very different, though. The book is more of an academic approach to memory management in .NET, while this course focuses on application. Suppose you are someone who likes the hands-on, application-heavy approach to learning a subject. I highly recommend this course. Before I took this course, I read the book, and I still found it incredibly useful and enlightening. If you found that the book was too heavy on theory, this is probably the content you are looking for.
