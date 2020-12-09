---
title: Why I Love F# for Mathematical Planning
date: 2020-12-09
draft: false
tag: design, fsharp
---

> A designer knows he has achieved perfection not when there is nothing left to add, but when there is nothing left to take away
> - Antoine de Saint-Exupery

On my journey of growing as a developer, I am consistently inspired by language features which seem incredibly simple but yield remarkable benefit. As I try to master F#, I am frequently surprised by how powerful the language is for expressing ideas while having so few features. Discussions frequently pop up about the need for ever more powerful abstractions, yet I find myself amazed by how far you can take the language with what is already there.

I am no programming language expert, but I admire languages that maintain a lean feature set. Every new feature added to a language makes it just a little bit more difficult to fully understand and a little more intimidating for new developers. It is an impressive design feat when a language can remain approachable for beginners but enable the flexibility that library authors need.

I am an Industrial Engineering turned Machine Learning Engineer, and I focus on the problem of maximizing the profitability and efficiency of companies. Often the solution involves a Mathematical Planning Model (aka Mathematical Programming). What I hope to do in the next few paragraphs is illustrate to you how some of the most basic features of F#, Discriminated Unions and Units of Measure, eliminate the most pernicious bugs when developing these models.

## The Domain of Mathematical Planning

The domain of Mathematical Planning is made up of Decisions, Constraints, and Objectives. A Decision is a choice that a business needs to make. It can be how many of Item X do we buy, do we build in Location A or Location B, or how many people do we assign to each job. Constraints are the rules we need to abide by. They are the limitations on what is possible. A Constraint could be that we only have 10 people available, or we can only build in Seattle or Portland, or we only have $1,000,000 to invest. The Objective is how we measure success. It is the function we want to maximize or minimize. We could minimize waste, maximize profit, or minimize cost.

Many of my colleagues are building their models with Python. Python is a great language and I have been productive with it in the past. Here is a snippet of what a mathematical planning model may look like in Python:

```python
# Define a list of items to optimize for
items = ["A", "B", "C"]

# Define a list of locations to assign items to
locations = ["Portland", "Seattle", "Detroit"]

# Define a dictionary of revenue associated with each item and location tuple
revenue = {("A","Portland"):1.5;, ("A","Seattle"):1.7 ... }

# Define a dictionary with the availability of each item
availability = {"A":10.0, "B":20.0, "C":14.0}

# Create a Decision for each Item, Location combination. This will be how much
# of a given item we decide to send to that location
allocation = LpVariable.dicts("AmountSent",(items,locations), 0)

# Create an instance of a `Problem` object and state that we want to maximize
# the objective we give it
problem = LpProblem("ItemAllocation", LpMaximize)

# We create an expression which evaluates the total revenue
revenue_expr =
    lpSum([revenue[i][l] * allocation[i][l] for i in items for l in locations])

# We set the Objective of the Problem by adding it
problem += revenue_expr, "MaximizeRevenue"

# For each item in items, create a constraint which states that the total number
# of items that is allocated cannot exceed the availability of the item
for i in items:
    problem += lpSum([allocation[l][i] for l in location] <= availability[i])

```

This is the beginning of a straightforward assignment problem. We have a list of items, `items`. For each `item` in `items`, we must decide how many we send to each `location` in `locations`. There is a limit on how much of each `item` is available for us to send. There is a revenue associated with sending a particular `item` to a given `location`. In this problem we want to maximize our revenue which is calculated by multiplying the `decision` for a given `item` and `location` by the `revenue` associated with it. Finally, we create a constraint for each `item` in `items` which states that the total number of a given `item` that is allocated cannot exceed the total that is available.


This is only part of the problem. Normally there would be more constraints that would make it more interesting. This is enough of a problem to illustrate my case though. There are two errors in this model already. If you were paying close attention you may have found one. I promise you cannot detect the second.

## The Power of Domain Modeling Using Discriminated Unions

F# provides two simple but powerful features which help ensure against the errors in the Python code. The first is Discriminated Unions. If we were to reformulate this problem using F#, the first thing we would do was define some simple types to model our domain.

```fsharp
type Item = Item of string
type Location = Location of string
```

Instead of just using strings to describe our Items and Locations, we create simple, single case Discriminated Unions (DU). These DUs provide context around what the strings are meant to represent. Let's go ahead and create our `item` and `locations` lists again. This time, wrapping them in DUs.

```fsharp
let items = 
  ["A"; "B"; "C"] 
  |> List.map Item

let locations = 
  ["Portland"; "Seattle"; "Detroit"]
  |> List.map Location
```

We will also update our `availability` information to use these new types.

```fsharp
let availability =
    [
        Item "A", 10.0
        Item "B", 20.0
        Item "C", 14.0
    ] |> Map
```

We will create the Decisions for each `item` and `location`. We store these `Decision` types in a `Map` which is indexed by an `(Item * Location)` tuple.

```fsharp
let allocation =
    DecisionBuilder<Servings> "AmountSent" {
        for i in items do
            for l in locations ->
                Continuous (0.0, infinity)
    } |> Map
```

We now attempt to create the same constraints we did in Python with a direct translation.

```fsharp
let allocationContraints =
    ConstraintBuilder "ItemLimit" {
        for i in items ->
            List.sum [for l in locations -> 1.0 * allocation.[l, i]] <== availability.[i]
    }   
```

Except, the compiler is gives us an error on the indexing of `allocation`.

![Compiler error for indexing Map](/img/2020-12-08-indexing-error.png)

What some of you may have noticed in the Python code is that the `allocation` collection is indexed by an `Item` then `Location`. The original code was trying to access it by `location` then by `item`. This would have thrown an error at runtime due to a missing value. In F# this becomes a compiler error. The type system itself it is helping you. This may seem small, but this is one of the most painful types of errors when debugging a Mathematical Planning model.

Someone may say that this can be accomplished in other languages and I would agree. I believe where F# is unique is in the simplicity and ease of using single case Discriminated Unions for wrapping primitives. It is virtually no additional effort.

## Units of Measure: The Achilles Heel of Numbers

There is an underappreciated problem in software development, and it is that numbers are rarely just numbers. They represent something: `cm`, `feet`, `kg`, or `meters`. It is rare that we care about a raw number. What we really concerned with is what the number represents. In most languages there are no easy mechanisms for tracking the Units of Measure associated with a number. F# on the other hand has baked the concept of a Unit of Measure into the type system.

The Units of Measure feature will reveal the second problem with the Python code that otherwise may remain undetected. Let's update our domain with some new types to track the units on our numbers.

```fsharp
[<Measure>] type Servings
[<Measure>] type Kg
```

We now have units to represent `Servings` and `Kg`. Let's update our `availability` collection to store numbers with these units attached.

```fsharp
let availability =
    [
        Item "A", 10.0<Kg>
        Item "B", 20.0<Kg>
        Item "C", 14.0<Kg>
    ] |> Map
```

We have now provided more context around our availability numbers. We now know they are stored in units of `Kg`. The F# compiler will enforce correct algebra as we work with them. We now update our Decisions to be in units of `Servings`. 

```fsharp
let allocation =
    DecisionBuilder<Servings> "AmountSent" {
        for i in items do
            for l in locations ->
                Continuous (0.0<Servings>, 1_000_000.0<Servings>)
    } |> Map
```

With our Decisions updated, we go back to our constraint definition and we now see a new bug.

![Units of Measure Mismatch](/img/2020-12-08-units-of-measure-mismatch.png)

The important part of this message is at the bottom. The compiler is complaining that the left-hand is in units of `Servings` and the right-hand side is in units of `Kg`. It does not make sense to compare values that are in different units, so the compiler is throwing an error. In other languages this error would go undetected. Worse, it may not even be caught in unit testing because the math will still work, it just won't give correct results.

Let's go ahead and add some conversion data so that we can fix this.

```fsharp
let itemMass =
    [
        Item "A", 1.1<Kg/Servings>
        Item "B", 2.0<Kg/Servings>
        Item "C", 0.7<Kg/Servings>
    ] |> Map
```

We now have data which will allow us to convert from `Serving` to `Kg`. Let's incorporate it into our constraint creation expression.

```fsharp
let allocationContraints =
  ConstraintBuilder "ItemLimit" {
    for i in items ->
      List.sum [for l in locations -> itemMass.[i] * itemAllocation.[i, l]] <== availability.[i]
  } 
```

Now the compiler is happy because the units are in `Kg` on both sides. This simple feature of ensuring correct Units of Measure eliminates what is possibly the most nefarious bug in Mathematical Planning. It would be hard to calculate the number of hours wasted on badly formulated models due to mismatched Units of Measure.

## Simple Building Blocks

F# is an incredibly expressive language while staying lean on the number of features. Other languages have taken the approach of throwing every possible feature in. F# is relatively slow to incorporate new features and they are always purposeful. Most of the time the feature is orthogonal to the rest of the language. This is keeping the language approachable for newcomers so the climb to mastery is not nearly as steep. I believe these two simple features, Discriminated Unions and Units of Measure, uniquely position F# as an awesome language for Mathematical Planning.
