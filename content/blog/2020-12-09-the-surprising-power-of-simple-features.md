---
title: The Surprising Power of Simple Features
date: 2020-12-09
draft: false
tag: design
---

> A designer knows he has achieved perfection not when there is nothing left to add, but when there is nothing left to take away
> - Anoine de Saint-Exupery

As I continue of my journey of growing as a developer, I an consistently inspired by language features which seem incredibly simple but yield incredible benefit. As I have continued to learn F#, I find myself continually suprised by how powerful the language is for expressing ideas while having so few features. Discussions frequently pop up about the need for ever more powerful abstractions yet I find myself pleasantly surprsised by how far you can take the language with what is already there.

I am no programming language expert but I admire languages that maintain a lean feature set. Every new features added to a language makes it just a little bit more difficult to fully understand and a little more intimidating for new developers. It is a really impressive design feat when a langauge can remain approachable for beginners but enable the felxibility that library authors need.

I am an Indusrial Engineering by training and I focus on the problem of maximizing the profitability and efficiency of systems. Most of the time the solution involves some mathematical model which identifies the best course of action. This is all based on the field of Mathematical Planning. What I hope to do in the next few paragraphs is illustrate to you how some of the most basic features of F#, Discriminated Unions and Units of Measure, eleminate the most prenicious bugs when developing these optimization models.

## The Domain of Mathematical Planning

The domain of Mathematical Planning is made up of Decisions, Constraints, and Objectives. A Decision is a choice that a business needs to make. It can be how many of Item X do we buy, do we build in Location A or Location B, or how many people do we assign to each job. Constraints are the rules we need to abide by, the limitations on what is possible. A Constraint could be that we only have 10 people available, or we can only build in Seattle or Portland, or we only have $1,000,000 to invest. The Objective is how we measure success. It is the function we want to maximize or minimize. We could minimize waste, maximize profit, or minimize cost.

Many of my colleagues are building their modesl with Python. Python is a great langauge and I have been productive with it in the past. I believe that F# provides some additional features that are of great value to my peers. Here is a snippet of what a mathematial planning model may look like:

```python
# Define a list of items to optimize for
items = ["A", "B", "C"]

# Define a list locations to assign items to
locations = ["Portland", "Seattle", "Detroit"]

# There is a revenue associated with each item and location tuple
revenue = {("A","Portland"):1.5;, ("A","Seattle"):1.7 ... }

# There is only so much of each item available
availability = {"A":10.0, "B":20.0, "C":14.0}

# We create a Decision for each Item, Location tuple. This will be how much
# of a given item we decide to send to that location
allocation = LpVariable.dicts("AmountSent",(items,locations), 0)

# We create an instance of a `Problem` object and state that we want to maximize
# the objective we give it
problem = LpProblem("ItemAllocation", LpMaximize)

# We create an expression which evaluates the total revenue
revenue_expr =
    lpSum([revenue[i][l] * allocation[i][l] for i in items for l in locations])

# We set the Objective of the Problem by adding it
problem += revenue_expr, "MaximizeRevenue"
```

This is the beginning of a striaghtforward assignment problem. We have a list of items, `items`. For each `item` in `items`, we must decide how many we send to each `location` in `locations`. There is a limit on how much of each `item` is available for us to send. There is also a revenue associated with send a particular `item` to a given `location`. In this problem we want to maximize our revenue which is calculated by multiplying the `decision` for a given `item` and `location` by the `revenue` associated with it.


This is only part of the problem. Normally there would be normally be more constraints that would make it more interesting. This is enough of a problem to illustrate my case though.