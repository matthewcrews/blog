---
title: Mathematical Programming with Machine Learning Models - Part 1
date: 2020-12-13
draft: false
tag: machine-learning, mathematical-planning, fsharp
---

One of the questions that I have to answer frequently is, "What is the difference between Mathematical Programming and Machine Learning?" This is an excellent question. The fields are close to one another and solutions often involve both techniques. The way I clarify it is that Mathematical Programming is primarily concerned with answering the question, "What should we do?" Machine Learning answers the question, "What is most likely?" We are often asking these questions at the same time which is why the techniques can become conflated.

I wanted to provide an example of a real world problem which involves the marriage of these two techniques. Due to the amount of material to cover, I decided to break it into two posts. This first post will setup the problem and show how we can use Mathematical Programming to find an optimal answer to the question, "What should we do?" Next week we will extend this work to incorporate a Machine Learning model which will be used as an input to the Mathematial Programming model.

## The Food Cart Problem

An example problem that I frequently use is the Food Cart Problem. It is easy for people to conceptualize. We are running a Food Cart and we want to know what items to pack at the beginning of the day to maximize our revenue. In this case we sell Burgers, Pizza, and Tacos (it's an eclectic Food Truck). Each food takes up a certain amount of pantry space, fridge, space, and weight. Our food truck is small so we have to be purposeful about what we pack. We have kept track of how much we sell each day while we are in inventory.

We feel that we can come up with a more profitable food packing plan. Right now we use a simple heuristic. On average, we sell 600 Burgers per day, 900 pizzas per day, and 700 tacos when we don't run out of inventory. Our food cart was recently downsized and has some restrictions now. We only have $3.0 m^3$ of storage, $2.0 m^3$ of fridge space, and can only carry $1,000 kg$ of weight. We have our same parking spot so we expect to see the same demand.

Below is a breakdown of the space and weight requirements per serving of the food we provide.

|    | Burger | Pizza | Taco | Available |
|----|--------|-------|------|---|
| Storage Space | $700.0 cm^3$ | $950.0 cm^3$ | $800.0 cm^3$ | $3.0 m^3$ |
| Fridge Space | $900.0 cm^3$ | $940.0 cm^3$ | $850.0 cm^3$ | $2.0 m^3$ |
| Weight Capacity | $550.0 gm$ | $800.0 gm$ | $600.0 gm$ | $1,000 kg$ |

## A Simple Heuristic

We decide to prioritize pizza because it makes the most money, then tacos, then burgers. We pack for 900 pizza orders because that's the average demand. With the remaining space, we pack tacos up until the average demand. If there is any room leftover we will pack some burgers. Given the dimensions of our food, the result comes to 900 pizzas, 466 tacos, and 0 burgers. This makes sense to us since we have much less space than we did before and we thought we may have to drop a food item from the menu. If we run the numbers, we expect to make $2,668.50 per day. Or do we...?

The important nuance to this problem is that this plan was made using the average demand. Half of the time the demand is higher, half of the time the demand is lower. On days where the demand for pizza is greater than 900, we would not be able to capture that additional revenue anymore because we do not have enough inventory. At the same time, when the demand for pizza is below 900 we loose out on the possibility we could have sold more of other foods had they been in stock We would like to get some idea of what the actual revenue value we should expect. Fortunately there is a tool for theis, [Monte Carlo Simulation](https://en.wikipedia.org/wiki/Monte_Carlo_method).

## Monte Carlo Simulation

Monte Carlo simulation is conceptually simple, you can get an idea for the shape of a distribution by sampling from it many times and then computing descriptive statistics. We can calculate the confidence intervals on our estimates so that we know how sure we are of the numbers. Our hope is to find what the average revenue is expected to be.

> **Note**: I encourage people to read up on Monte Carlo. I do not have the space here to provide an exhuastive explantion. To my Statistician readers, I know I am glossing over details. I am trying to provide an engaging example without intimidating readers with mathematical rigor.

Now, if you can remember your course on probability distributions, you may remember that the arrival rate of customers can be modeled by a [Poisson Distribution](https://en.wikipedia.org/wiki/Poisson_distribution). We will assume that this is a valid distribution to model the demand that we see at our food cart. Part of what makes this distribution easy to work with is that it takes a single parameter, $\lambda$. The $\lambda$ is easy to calculate, it is the mean of the data. Therefore we say that pizza demand is a Poisson distribution with a $\lambda$ of 900.0 or in math notation, $\text{PizzaDemand} \sim Poisson(\lambda = 900 )$. $\lambda$ can be thought of as the average rate of arrivals or in our case the overage number of orders in a day for a given food.

Now we need to do some domain modeling and setup our little simulation. We will start with some types to describe our domain.


```fsharp
type Food = Food of string
type DemandRate = DemandRate of float
```

The `Food` type differentiates between which food item we are referring to. The `DemandRate` is the average demand we have observed and is the input for our distributions, $\lambda$. We will use Units of Measure to track the units in our calculations. This will protect us from making silly conversion errors.

```fsharp
[<Measure>] type USD
[<Measure>] type cm
[<Measure>] type gm
[<Measure>] type serving
```

`USD` stands for United States Dollars. `cm` and `gm` are the SI units for volume and mass. The `serving` measure is for the quantity of servings we are packing. We will be using the `Math.NET` library for performing calculations with distributions. We need a function for taking a plan and generating a random outcome. We will call this functions many, many times to get an idea of the distribution of revenue.

```fsharp
let sample 
    (foodDemands: seq<Food * DemandRate>)
    (revenue: SMap<Food, float<USD/serving>>)
    (plan: Map<Food, float<serving>>)
    (rng: System.Random) =
    
    let evaluteSoldQuantity planAmount (DemandRate demandRate) rng =
        // Generate a random sample from the Poisson distribution and take the lesser
        // of the planned inventory or of the random Demand value that was generated
        let actualQuantity = Math.Min (float planAmount, Sample.poisson demandRate rng |> float)
        // Multiply by 1.0<serving> to get the correct units on the result
        actualQuantity * 1.0<serving>

    foodDemands
    |> Seq.map (fun (food, demandRate) -> food, (evaluteSoldQuantity plan.[food] demandRate rng))
    |> Seq.sumBy (fun (food, soldQuantity) -> soldQuantity * revenue.[food])
```

What `sample` does is take the plan and perform a single simulation. It generates a random demand value for the given food and compares that to what we planned to have in inventory. It takes the lesser of our planned quantity or the random demand that was generated which becomes the actual amount sold. It takes the quantity of each food that was sold and multiplies it by the revenue of the food. It sums this across all the foods which gives us our revenue for that single simulation. We create an `evaluate` function which will call the `sample` function many times and gather the results. `evaluate` then computes the descriptive statistics and the revenues that were generated in our simulations.

```fsharp
let evalute 
    (foodDemands: seq<Food * DemandRate>)
    (revenue: SMap<Food, float<USD/serving>>)
    (plan: Map<Food, float<serving>>)
    (rng: System.Random)
    (numberSamples: int) =

    let samples =
        seq {
            for _ in 1..numberSamples ->
                sample foodDemands revenue plan rng
                |> float
        } |> Array.ofSeq

    DescriptiveStatistics samples
```

We provide an argument to `evaluate` called `numberSamples` which controls the number of simulations that we perform. We then use function from the `Math.NET` package to compute the descriptive statistics, `DescriptiveStatistics`. This will provide us with the sample mean, variance, and standard deviation. We will then be able to compute the confidence internal to ensure we have an accurate estimate of the revenue we expect to achieve.

## A Simple Hueristic

