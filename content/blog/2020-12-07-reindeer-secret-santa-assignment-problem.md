---
title: Reindeer Secret Santa Assignment Problem
date: 2020-12-06
draft: true
---

I was having a chat with a friend about what types of problems are good candidates for Mathematical Planning. He posed the question, "Would a Secret Santa exchange be a good candidate?" At first I thought, no. As we kept chatting though, I changed my mind. This is actually a great problem for Mathematical Planning.

For those who are not familiar with what a Secret Santa exchange is, it is when a group of people get together and all put their names in a hat. Everyone draws out a name. You then buy a gift for the person who's name you drew. Normally everyone would get back together at a party and exchange fits. The fun part is that you don't know who is giving you a gift so it is a double surprise.

I initially did not think that this was a good candidate for Mathematical Planning because I didn't see a quantifiable objective. There was no way to measure the difference in quality of the different pairings. All valid pairings are equally as good. Normally you would use [Constraint Programming](https://en.wikipedia.org/wiki/Constraint_programming) and/or [SAT Solvers](https://en.wikipedia.org/wiki/Boolean_satisfiability_problem#Online_SAT_solvers) for these problems. SAT Solvers and Constraint Programming is interested in answering the question, "Is there an answer which satisfies these constraints?" versus Mathematical Planning which asks, "What is the BEST answer which satisfies these constraints?" The difference seems small but the problem is wildly different. Find the **best** answer to a problem is more difficult than finding just any answer which fits.

Our conversation continued and my mind was still turning. A new piece of information dropped. This family does Secret Santa every year. *Wait a minute*, I thought. *Don't we want an answer which pairs you with new people each year? Wouldn't it be better if you had someone different than you had the year before or the year before that?*. This problem just became a Mathematical Planning problem!

## A Reindeer Gift Exchange

I decided to embrace a bit of whimsy in this example and instead of people exchanging gifts, I decided to make it Reindeer. I mean, wouldn't Santa's reindeer want gifts as well? I begin modeling my problem by creating a simple domain to describe my problem.

```fsharp
type Reindeer = Reindeer of string
type Giver = Giver of Reindeer
type Receiver = Receiver of Reindeer
type SecretSanta = {
    Reindeer : Reindeer
    PreviousReceivers : Receiver list
}
```

I assume I am going to get the names of the Reindeer as `string` so I wrap them in a single case Discriminated Union to provide context to the data. The `Reindeer` will be both givers and receivers so I make additional types to represent the direction of relationships: `Giver` and `Receiver`. The `SecretSanta` type holds the data about the particular `Reindeer` in the `Reindeer` field and a list of reindeer they have been paired with in years past in the `PreviousReceivers` field. The `PreviousReceivers` is an ordered list where the first element was the last reindeer the `SecretSanta` gave a gift to. Our ideal solution has reindeer giving a gift to a reindeer they have not given to recently.

Now I create a function which takes a list of `SecretSanta`, builds the model, solves it, and returns the assignments. I will call the function `findAssignments`.

```fsharp
let findAssignments (santas:SecretSanta list) =
```

I now want to get the list of reindeer that I will be working with and create sets of `Giver` and `Receiver`. This reason for storing this data in a `Set` will become apparent in a few moments.

```fsharp
let reindeer =
    santas
    |> List.map (fun s -> s.Reindeer)
    |> Set

let givers = reindeer |> Set.map Giver
let receivers = reindeer |> Set.map Receiver
```

I now need to create the penalty values for assigning a reindeer to a reindeer whom they have given a gift recently. I will do this by using the `List.mapi` function which allows me to iterate through the list with the index value of the item. I use a simple heuristic for calculating the penalty.

$$\text{penalty} = \text{NumberOfPreviousRecipients}-\text{index}$$

What this does is provide a high penatly cost for assigning a reindeer to the reindeer they just gave a gift to. From there the cost keeps going down. I will store the result in a `SMap2` that will be indexed by the `Giver` type in the first dimension and the `Receiver` type in the second dimension. I have found using simple Discriminated Unions as a powerful tool for tracking how data is indexed.

```fsharp
let penalty =
    [ for s in santas do
        // Get the number of receivers once
        let numberOfReceivers = s.PreviousReceivers.Length
        s.PreviousReceivers
        |> List.mapi (fun idx r -> ((Giver s.Reindeer), r), float (numberOfReceivers - idx))
    ] |> List.concat
    |> SMap2
```

I now want to create the possible assignments. The key thing here is that it should not be possible to assign a reindeer to give a gift to itself. This is why I stored this data in `Set`