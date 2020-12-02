---
title: Modeling Non-Linear Functions with Flips
date: 2020-12-01
draft: true
---

Recently I was asked if it would be possible to add the `log` function to the [Flips library](https://flipslibrary.com). Flips is a library for modeling and solving Linear and Mixed-Integer Programming problems. Both of these classes of problems are constrained to only having linear (i.e. straight) lines. You may ask, "What do you mean by straight?" The following are examples of linear functions.

$$
\displaylines{
\text{Linear Functions}\\\\
y=1.0x+2.0 \\\\
y=2.0x_{1}+3.0x_{2} \\\\
y=1.2x_{1}+1.7x_{2}+x_{3}
}
$$

The following are non-linear functions.

$$
\displaylines{
\text{Non-Linear Functions} \\\\
y=1.0x^2+2.0 \\\\
y=2.0/x_{1}+3.0x_{2} \\\\
y=1.2x_{1}+1.7x_{2}\times x_{3}
}
$$

For a function to be linear in the domain of Linear/Mixed-Integer Programming the variables can only be added, subtracted, or multiplied by a coefficient. The reason this is important is because a Solver takes advantage of this structure which searching for solutions.

## What if we need a Non-Linear Function

Fortunately we have ways of working around this limitation. Another way to think of a curve is just a series of straight lines. We could approximate our curve using a series of straight lines that were close enough to the original function to make our answer meaningful. For this example let's try modeling the parabola $y=-x^2+10.0$. We will use this to represent the Objective Function.


```fsharp
open Flips
open Flips.Types
open Flips.SliceMap

// The Range of values we want to consider and the index for the value
let valueRange =
    [-5.0..0.5..5.0]
    |> List.mapi (fun index value -> index, value)

// We will need the indices for the vertices of our lines
let indices = valueRange |> List.map fst

// Create a decision variable for each point
let decs =
    DecisionBuilder "Amount" {
        for i in indices ->
            Continuous (0.0, 1.0)
    } |> SMap


let valueMap = SMap valueRange

// We create a constraint saying that we must 
let totalOneConstraint = Constraint.create "TotalValue" (sum decs == 1.0)

// We create an indicator variable which corresponds to pairs of points
// on the line we are modeling
let pairDecisions =
    DecisionBuilder "Pair" {
        for pair in List.pairwise indices ->
            Boolean
    } |> SMap

// We state that if we want to use the pair of vertices,
// the indicator variable associated with that pair must
// be on as well
let pairConstraints =
    ConstraintBuilder "UsePair" {
        for KeyValue ((i, j), d) in pairDecisions ->
            decs.[i] + decs.[j] >== d
    }

// A constraint stating that only one pair may be used
let onlyOnePair = Constraint.create "OnlyOnePair" (sum pairDecisions == 1.0)

// We create a expression that is an approximation of our parabola
let costExpression =
    List.sum [for (i, v) in valueRange -> (-1.0 * v ** 2.0 + 10.0) * decs.[i] ]

// We creat an objective which is to maximize our expression
let objective = Objective.create "MaxValue" Maximize costExpression

// We create a model and add the constraints
let model =
    Model.create objective
    |> Model.addConstraints pairConstraints
    |> Model.addConstraint onlyOnePair
    |> Model.addConstraint totalOneConstraint

// We are only using basic settings
let settings =  Settings.basic

// We attempt to solve the problem and return the result
let result = Solver.solve settings model

// If the result is a success, we print out the value of the expression
// we were maximizing
match result with
| Optimal solution ->
    printfn "Objective Value: %f" (Objective.evaluate solution objective)
    printfn "%A" solution.DecisionResults
```
