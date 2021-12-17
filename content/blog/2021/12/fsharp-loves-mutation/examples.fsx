// let inline checkNonNull argName arg =
//     if isNull arg then
//         nullArg argName

// Maximum value in array
let mutationFreeMax (x: array<int>) =
    // I am must going to assume x is non-null and not empty

    let rec loop (acc: int) (idx: int) (array: array<int>) =
        if idx < array.Length then
            let curr = array.[idx]
            if curr > acc then
                loop curr (idx + 1) array
            else
                loop acc (idx + 1) array
        else
            acc

    loop x.[0] 1 x


let values = [|1 .. 10|]
let test1 = mutationFreeMax values


let mutationMax (x: array<int>) =
    // I am must going to assume x is non-null and not empty
    let mutable acc = x.[0]
    let mutable idx = 1

    while idx < x.Length do
        let curr = x.[idx]
        if curr > acc then
            acc <- curr
        idx <- idx + 1

    acc

let test2 = mutationMax values