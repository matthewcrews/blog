# Who has Inspired You?


I was recently asked on Twitter, "What, who, have influenced how you think about programming and have helped inspire you and keep you humble?" This was in response to my saying that I am baffled at how bad I was at programming at the beginning of my career but still successful. I felt that the question warranted more space than Twitter provides. The following are the key people in my career who have helped me grow and inspire me. They are roughly in chronological order.

## Justin

My first real job out of college was as an Industrial Engineer at Siltronic AG. I worked in the Supply Chain group and it was my responsibility to increase the efficiency of the facility. The facility was a 200mm silicon wafer manufacturer. At the time much of the industry had already moved to 300mm but there were many legacy facilities that still worked with 200mm. This meant that we had to maximize efficiency since there were not going to be significant capital investments because 200mm was not a growth part of the industry.

It quickly became apparent to me that the scheduling algorithms being used to feed the line were leading to too much work in progress (WIP) which caused Cycle Times to balloon. Long Cycle Times meant that metrology feedback was slow so it was easy to have quality excursions, creating a rework. We had to get Cycle Times down, which meant that the WIP also had to go down. Leadership feared that we couldn't maximize the utilization of critical tools unless we had significant WIP piled up throughout the facility. I reassured leadership that with the right algorithms in place, we could reduce WIP but still maintain high utilization of key bottlenecks in the fab.

The problem was that we needed a programmer to write the algorithms. The IT department was swamped with support work, so none of their programmers were available. I figured that I could write the algorithm, but I had no idea how to integrate it into the software running the fab. Enter Justin. Justin was the programmer for the Supply Chain group. He was a self-taught programmer who had worked his way up from working in the fab to being a developer. He had learned Groovy, Grails, and SQL and had built many of the tools the Supply Chain group used to manage the fab. I began pestering him with questions and he patiently helped me build the tools we needed in Groovy.

Justin was the first developer mentor I had. Without his patience and guidance, I would not be where I am today. I owe a great deal of my success to his kindness and patience. If only we could all have a mentor like Justin in our lives. We successfully halved the facility’s WIP and Cycle Time and got awards for our work. I was flown out to the headquarters in Germany for an Industrial Engineering Symposium where I was asked to present on what we had done. None of that success would have been possible without Justin. I am grateful that I am friends with Justin to this day.

## Don Syme

While Groovy may have been the first language I did any productive work in, it wasn't till F# that I turned into a real developer. I had moved on from Siltronic and now worked at an online retailer where we specialized in selling on the Amazon marketplace. We used R to calculate statistical models of the demand, supply, and pricing fluctuations of a large number of products. We would then use the results of these models to optimize our ordering to maximize profitability. The problem we kept running into though was that R was not designed as an industrial-strength programming language. R is a great statistics calculator. It's a poor production language.

I remember having dabbled with the F# language several years prior and thought that the Units of Measure feature would be helpful in all of our financial modeling. I decided to pick it back up and see how difficult it would be to translate our R code to F#. Turns out that F# really shines at data transformation work. We were able to build fast and robust services in a relatively short period. Our entire data pipeline was transformed into F# within a few weeks. It ran faster and more reliably than anything previous.

It was at this point that I start taking software development as a discipline more seriously. I start reading everything I can on F# and on Don Syme, the creator of F#. Before long I learn that he was primarily responsible for Generics in .NET. What struck me, though, was his humility. In all the interviews I have watched or listened to, I have never sensed an ounce of pride coming from the man. He is not brash or abrasive. He does not lord his accomplishments over others. He appears to have infinite patience with people continually asking for Higher-Kinded Types in F#.

For all of his technical accomplishments, it's Don's character that most impresses me. He is the model of a brilliant developer who is still kind and approachable. F# has benefited tremendously from his leadership, and I hope to emulate how he treats other developers.

## Rich Hickey

While I have never written any Clojure, I benefited from the presentations that Rich Hickey has made. My favorite is one he gave at Rails Conf titled [Simplicity Matters](https://www.youtube.com/watch?v=rI8tNMsozo0). I have found all of his talks insightful and have opened my mind to new ways of thinking about software development and what we are fundamentally doing at the end of the day.

## Mark Seemann

When I was early on in my exploration of Functional Programming, I found the blog of Mark Seemann. Mark has a way of simplifying complex concepts down to concrete terms that are easy to understand. One of my favorite memories was at the first NDC Minnesota conference. The night before the conference began there was an option to go on a boat tour of St. Paul and have dinner with the speakers. Mark was on the boat and I ended up getting to sit and chat with him for a couple of hours. He was kind and gracious. I remember mentioning to him my struggle with understanding what a Monad is and he said, "It's just SelectMany." At that moment, an explosion went off in my mind, and I was like, "Oh, wow. Yeah, that's all it is!" Mark has always inspired me with his ability to find the right bridge to show people to help them grasp complex concepts.

## Scott  Wlaschin

Every F# developer will eventually go on a pilgrimage to ["F# for Fun and for Profit"](https://fsharpforfunandprofit.com/). I haven't met an F# dev who hasn't spent some time on Scott's website. Scott has an incredible ability to translate scary concepts into simple ideas. I place Scott and Mark in the same category of brilliant developers who know how to explain things to us, newbies. Scott is a treasure and one of the key people who has made F# approachable.

## John Carmack

John Carmack needs no introduction. He is considered by many to be one of the best developers alive. If you haven't read the book "Masters of DOOM,” do yourself a favor and buy it. The audio version is narrated by Will Wheaton, who does a brilliant job. There is one quote that has had the most impact, though. It comes from an interview that Lex Fridman did with John. The whole thing is fantastic (5 hours!?!?), but the quote comes from [5:07:00](https://youtu.be/I845O57ZSy4?t=18429).

> “You can’t learn everything, but you have to convince yourself that you can learn anything...”
> - John Carmack

The implications of this are massive. Yes, it is insane to think that you can learn everything. Yet, you are fully capable of learning anything, should you choose to apply yourself. It may be difficult. It may take a long time, but it is possible.

## Jonathan Blow

Ah, now it's getting spicy. Jonathan Blow is a divisive figure. Some people love him; others hate him. He has strong opinions on the state of the software industry and has no problem calling things bullshit. In some ways, Jonathan Blow is the polar opposite of Don Syme. I have a hard time thinking about two people with personalities that are more different. Now, I'm not going to tell you how to think about Jonathan Blow; you are entitled to your opinion, but here's how he has helped me.

As I have been on my journey to learn to write high-performance code, I inevitably found myself looking to game engine development for inspiration. I also research various programming languages to draw inspiration for how to write F#. Eventually, I find Jonathan Blow, who is working on Jai. I don't know if this is a character flaw or not, but I appreciate people who are willing to speak their minds frankly. I have spent so much time in corporate America and politicking that I find it exhausting. So here is this Jonathan Blow character saying all this Object Oriented (OO) stuff is bullshit and I was like, "Wait? Really? You can say that?"

I cannot tell you how dumb OO has made me feel. I despaired early in my career because I failed any time I tried to learn OO or design with OO. I thought I was too dumb to be a "real" programmer. So when I found this Jonathan Blow guy who is clearly very intelligent, blasting OO, I felt liberated. "Maybe I'm not dumb? Maybe I can be a real programmer?"

I don't agree with Jonathan Blow on many points, but he gave me hope that maybe I wasn't the problem and that maybe OO isn't the best way to compose software. I wrote him several questions years ago. I didn't expect to hear back from him. To my surprise, I got a long response answering my questions in detail. It was an incredibly kind and gracious letter—nothing like the bluster that I see on YouTube.

## Casey Muratori

Alright, let's finish off this list with some additional spice. Casey is famous (infamous?) for speaking out against the insanity that has gripped a great deal of the software industry. His video [Clean Code, Horrible Performance](https://www.youtube.com/watch?v=tD5NrevFtbU) made Twitter fun for a few days. In fact, the response video I did to his is still the most watched thing I have made.

Where Casey shines, though, is his enormous generosity. He has poured countless hours into creating the [Handmade Hero](https://handmadehero.org/) video series to teach people to write a game engine from scratch. He also went ahead and created a course to learn [Performance-Aware Programming](https://www.computerenhance.com/). Creating educational material is incredibly difficult and time-consuming. It is also typically not a high-paying endeavor.

For all of Casey's technical chops, what really impresses me is his willingness to offer solutions to problems. Instead of just griping about the state of the industry, he actively creates educational material to help people. He puts in the work to help solve the problem.

A large part of why I created the "Fast F#" YouTube channel is because Casey inspired me. Instead of just being frustrated with slow F# code, offer tools and resources for people to learn to write fast code. Be the solution.

## Conclusion

There are many more people who have helped and inspired me along the way. I am grateful for the F# community and their patience with my insane questions. There's a joke in the F# community that people come for the language but stay for the people. I have found this to be true. F# is not a perfect language, not by a long shot. But it is a great language, and there is an incredible community around it.

