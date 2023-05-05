---
title: What is Going On With Software?
date: 2023-05-04
draft: false
tag: language-design, performance
---

I have been puzzling over programming languages and performance and I want to put my thoughts out and invite feedback. We are at an interesting place in the software industry. Software development, as a profession, is not even a century old yet, but there are many strong opinions on the "right" way to write software. Object Oriented Programming (OOP) is the dominant paradigm but Functional Programming (FP), or at least Functional Style, has seen increased interest. I also have a narrow view of the industry so perhaps my perceptions are off.

While our hardware has never been faster, the software is often derided as slow and buggy, and rightly so. CPUs have grown in performance by several orders of magnitude, but we don't feel that when we use the software. Some say that we are just tackling more complex problems and this is the price that we pay. I'm not sure the difficulty of our problems has grown at the same pace as computer performance.

For perspective, the Intel 386 is a consumer chip that existed when I was a child. It had a single core that ran at up to 40 MHz. Today, I am writing this on a PC with an Intel 13900K with 8 Performance cores that can sustain 5.4 GHz together. Taken together, that gives a theoretical performance improvement of over 1,000x. That says nothing of the advances in chip design when it comes to branch prediction, out-of-order execution, cache improvements, and SIMD. Are we really tackling problems that are over 1,000x more difficult than we were a few decades ago? Really?

## The Siren Call of Abstraction

My perception of what happened is that programmers got expensive, so businesses wanted tools that made developers more productive. Languages were created that promised to make developers more productive. Early on this looked like the C programming language which freed the developer from thinking about the particular machine assembly they were programming for. Later we added Polymorphism and Interfaces so we could abstract over types. Along the way, we decided we didn't want to worry about memory so we added technologies for automatic memory management with Garbage Collectors and Reference Counting. We then gained the ability to create and share libraries so we could just build on someone else’s abstractions.

We just kept adding abstractions to continue to "simplify" things for developers. In the meantime, almost all awareness of the physical hardware our software is running on has been lost. It has gotten to the point where I regularly talk with developers who have no concept of the memory hierarchies inside an x86 chip. And you know what? I don't blame them. The vast majority of the industry says it doesn't matter. CPUs are fast, and compilers are good, so all that magic will make the problem disappear.

What strikes me odd about that is that in no other engineering discipline do they abstract away physical reality when they think about the problem. Civil Engineers designing a bridge must consider the terrain where the bridge will be built. Industrial Engineers take into account a building’s physical layout when designing workstations. Chemical Engineers consider the ambient environment when designing processes so that the chemistry actually works. Only in Software Engineering do we ignore the physical limitations of our systems.

The problem is that ignoring reality and embracing abstraction worked. It worked really well. Programmers could crank out features faster and managers were delighted. We were still riding the wild gains in performance that chip makers were cranking out so if something was slow today, it would be fast enough in 6 months. We were drinking deeply of the wine of Abstraction and times were good.

## Abstraction Hangover

Fast forward a couple of decades and we are no longer seeing these leaps in hardware performance but the software industry is still hooked on the drug of abstraction. The old adage of, "Every problem in computer science is solved with another layer of abstraction," is still going strong. And as much as I would like to deride this, it's done much good. I never would have become a developer if it wasn't for F# which is far removed from physical hardware. Tools like Excel and VBA have enabled countless people to write just a little bit of code to make their lives enormously better. We cannot deny that a significant amount of good has come from this.

There is a real problem, though. Software is getting slower and buggier. It is becoming more and more of a pain to interact with software systems. We have schools that [can't turn off their lights](https://www.businessinsider.com/massachusetts-high-school-lights-wont-turn-off-costing-taxpayers-thousands-2023-1) due to bugs in the software. We had an upset in the JavaScript ecosystem when the [left-pad](https://www.businessinsider.com/npm-left-pad-controversy-explained-2016-3) library was removed from NPM which is an example of over-reliance on someone else’s abstraction. Perhaps we need to introspect about the quality of the foundations we are building on. These abstractions have been built up so high that a single person cannot understand them all. The software developer field has fractionated into a myriad of separate disciplines and areas of focus.

## Personal Reflection

When I think about my own career and look at the software development industry I am concerned. I am concerned because I don't want to add to this mess. I want to be helping. I want to write quality software. I am asking myself, "Am I addicted to Abstraction?" This is a difficult thing for me to unpack because my ability to feed my family is currently tied to my ability to write software. It doesn't surprise me when people bristle when Casey Muratori calls out Clean Code as bad for performance. It's no surprise that Jonathan Blow gets so much heat when he goes on one of his tirades about the state of the industry. They are rightly pointing out that the software industry is in a dismal place but people’s well-being is depending on their developer job so they lash back. We’ve overbuilt our abstractions but we are all still being paid so we don’t want to upset the boat.

> **Aside**: I'm a fan of Casey and Jon. I don't know them personally, but I've learned a great deal from them.

At the same time, I am a beneficiary of these abstractions. I wouldn't have my job without them. I tried learning C years ago and it was brutal. C++ hurt me so badly that I swore I’d never write it again. I don't think the solution is for everyone to return to C. I don't think we just throw out all of the progress and innovation that has occurred. Perhaps we need to make it easier for people to work at a low level. This is why I am a fan of Rust because it seems to be a new way to think about how we let people work at a low level safely. Personally, I'm working on how to make F# a language to write high-performance code in an easy and concise way. I constantly strive to write code that is simple and fast. That works in harmony with the CPU, not just ignoring it.

I like how .NET has been taking performance more seriously, and I hope that trend continues. I think we need to see a renaissance in software development where we start peeling back some of these layers of abstraction. We need to make it easier to manage memory. We need to explore new paradigms which allow us to work in harmony with the hardware and not just abstract it away.
