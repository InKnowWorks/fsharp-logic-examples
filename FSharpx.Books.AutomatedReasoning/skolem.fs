﻿// ========================================================================= //
// Copyright (c) 2003-2007, John Harrison.                                   //
// Copyright (c) 2012 Eric Taucher, Jack Pappas, Anh-Dung Phan               //
// (See "LICENSE.txt" for details.)                                          //
// ========================================================================= //

module FSharpx.Books.AutomatedReasoning.skolem

open intro
open formulas
open prop
open fol

// ========================================================================= //
// Prenex and Skolem normal forms.                                           //
// ========================================================================= //
//
// pg. 140
// ------------------------------------------------------------------------- //
// Routine simplification. Like "psimplify" but with quantifier clauses.     //
// ------------------------------------------------------------------------- //

let simplify1 fm =
    match fm with
    | Forall (x, p) ->
        if mem x (fv p) then fm else p
    | Exists (x, p) ->
        if mem x (fv p) then fm else p
    | _ ->
        psimplify1 fm

let rec private simplifyImpl fm cont =
    match simplify1 fm with
    (* Cases which need to be recursively simplified. *)
    | Not p ->
        simplifyImpl p <| fun p' ->
            cont (simplify1 (Not p'))
    | And (p, q) ->
        simplifyImpl p <| fun p' ->
        simplifyImpl q <| fun q' ->
            cont (simplify1 (And (p', q')))
    | Or (p, q) ->
        simplifyImpl p <| fun p' ->
        simplifyImpl q <| fun q' ->
            cont (simplify1 (Or (p', q')))
    | Imp (p, q) ->
        simplifyImpl p <| fun p' ->
        simplifyImpl q <| fun q' ->
            cont (simplify1 (Imp (p', q')))
    | Iff (p, q) ->
        simplifyImpl p <| fun p' ->
        simplifyImpl q <| fun q' ->
            cont (simplify1 (Iff (p', q')))
    | Forall (x, p) ->
        simplifyImpl p <| fun p' ->
            cont (simplify1 (Forall (x, p')))
    | Exists (x, p) ->
        simplifyImpl p <| fun p' ->
            cont (simplify1 (Exists (x, p')))

    (* This formula can't be simplified any further. *)
    | fm ->
        cont fm

let simplify fm =
    simplifyImpl fm id

// pg. 141
// ------------------------------------------------------------------------- //
// Negation normal form.                                                     //
// ------------------------------------------------------------------------- //

let rec private nnfImpl fm cont =
    match fm with
    | And (p, q) ->
        nnfImpl p <| fun nnf_p ->
        nnfImpl q <| fun nnf_q ->
            cont (And (nnf_p, nnf_q))
    | Or (p, q) ->
        nnfImpl p <| fun nnf_p ->
        nnfImpl q <| fun nnf_q ->
            cont (Or (nnf_p, nnf_q))
    | Imp (p, q) ->
        nnfImpl (Not p) <| fun nnf_not_p ->
        nnfImpl q <| fun nnf_q ->
            cont (Or (nnf_not_p, nnf_q))
    | Iff (p, q) ->
        nnfImpl p <| fun nnf_p ->
        nnfImpl q <| fun nnf_q ->
        nnfImpl (Not p) <| fun nnf_not_p ->
        nnfImpl (Not q) <| fun nnf_not_q ->
            cont (Or (And (nnf_p, nnf_q), And (nnf_not_p, nnf_not_q)))
    | Not (Not p) ->
        nnfImpl p cont
    | Not (And (p, q)) ->
        nnfImpl (Not p) <| fun nnf_not_p ->
        nnfImpl (Not q) <| fun nnf_not_q ->
            cont (Or (nnf_not_p, nnf_not_q))
    | Not (Or (p, q)) ->
        nnfImpl (Not p) <| fun nnf_not_p ->
        nnfImpl (Not q) <| fun nnf_not_q ->
            cont (And (nnf_not_p, nnf_not_q))
    | Not (Imp (p, q)) ->
        nnfImpl p <| fun nnf_p ->
        nnfImpl (Not q) <| fun nnf_not_q ->
            cont (And (nnf_p, nnf_not_q))
    | Not (Iff (p, q)) ->
        nnfImpl p <| fun nnf_p ->
        nnfImpl (Not q) <| fun nnf_not_q ->
        nnfImpl (Not p) <| fun nnf_not_p ->
        nnfImpl q <| fun nnf_q ->
            cont (Or (And (nnf_p, nnf_not_q), And (nnf_not_p, nnf_q)))
    | Forall (x, p) ->
        nnfImpl p <| fun nnf_p ->
            cont (Forall (x, nnf_p))
    | Exists (x, p) ->
        nnfImpl p <| fun nnf_p ->
            cont (Exists (x, nnf_p))
    | Not (Forall (x, p)) ->
        nnfImpl (Not p) <| fun nnf_not_p ->
            cont (Exists (x, nnf_not_p))
    | Not (Exists (x, p)) ->
        nnfImpl (Not p) <| fun nnf_not_p ->
            cont (Forall (x, nnf_not_p))
    | fm ->
        cont fm

let nnf fm =
    nnfImpl fm id


// pg. 143
// ------------------------------------------------------------------------- //
// Prenex normal form.                                                       //
// ------------------------------------------------------------------------- //

// OPTIMIZE : Optimize using CPS.
let rec pullquants fm =
    match fm with
    | And (Forall (x, p), Forall (y, q)) ->
        pullq (true, true) fm mk_forall mk_and x y p q
    | Or (Exists(x, p), Exists(y, q)) ->
        pullq (true, true) fm mk_exists mk_or x y p q
    | And (Forall (x, p), q) ->
        pullq (true, false) fm mk_forall mk_and x x p q
    | And (p, Forall (y, q)) ->
        pullq (false, true) fm mk_forall mk_and y y p q
    | Or (Forall (x, p), q) ->
        pullq (true, false) fm mk_forall mk_or x x p q
    | Or (p, Forall (y, q)) ->
        pullq (false, true) fm mk_forall mk_or y y p q
    | And (Exists (x, p), q) ->
        pullq (true, false) fm mk_exists mk_and x x p q
    | And (p, Exists (y, q)) ->
        pullq (false, true) fm mk_exists mk_and y y p q
    | Or (Exists (x, p), q) ->
        pullq (true, false) fm mk_exists mk_or x x p q
    | Or (p, Exists (y, q)) ->
        pullq (false, true) fm mk_exists mk_or y y p q
    | _ -> fm

and pullq(l,r) fm quant op x y p q =
    let z = variant x (fv fm)
    let p' = if l then subst (x |=> Var z) p else p
    let q' = if r then subst (y |=> Var z) q else q

    op p' q'
    |> pullquants
    |> quant z


let rec private prenexImpl fm cont =
    match fm with
    | Forall (x, p) ->
        prenexImpl p <| fun p' ->
            cont (Forall (x, p'))
    | Exists (x, p) ->
        prenexImpl p <| fun p' ->
            cont (Exists (x, p'))
    | And (p, q) ->
        prenexImpl p <| fun p' ->
        prenexImpl q <| fun q' ->
            cont (pullquants (And (p', q')))
    | Or (p, q) ->
        prenexImpl p <| fun p' ->
        prenexImpl q <| fun q' ->
            cont (pullquants (Or (p', q')))
    | _ ->
        cont fm

let prenex fm =
    prenexImpl fm id

let pnf fm =
    simplify fm
    |> nnf
    |> prenex

// pg. 146
// ------------------------------------------------------------------------- //
// Get the functions in a term and formula.                                  //
// ------------------------------------------------------------------------- //

let rec funcs tm =
    match tm with
    | Var x -> []
    | Fn (f, args) ->
        List.foldBack (union << funcs) args [f,List.length args]

let functions fm =
    atom_union (fun (R (p, a)) -> List.foldBack (union << funcs) a []) fm

// pg. 149
// ------------------------------------------------------------------------- //
// Core Skolemization function.                                              //
// ------------------------------------------------------------------------- //

let rec private skolemImpl fm fns cont =
    match fm with
    | Exists (y, p) ->
        let xs = fv fm
        let f = variant (if xs = [] then "c_" + y else "f_" + y) fns
        let fx = Fn (f, List.map Var xs)
        skolemImpl (subst (y |=> fx) p) (f :: fns) cont
    | Forall (x, p) ->
        skolemImpl p fns <| fun (p', fns') ->
            (Forall (x, p'), fns')
            |> cont
    | And (p, q) ->
        skolem2Impl And (p, q) fns cont
    | Or (p, q) ->
        skolem2Impl Or (p, q) fns cont
    | _ ->
        cont (fm, fns)

and skolem2Impl cons (p, q) fns cont =
    skolemImpl p fns <| fun (p', fns) ->
    skolemImpl q fns <| fun (q', fns) ->
        (cons (p', q'), fns)
        |> cont

let skolem fm fns =
    skolemImpl fm fns id

let skolem2 cons (p, q) fns =
    skolem2Impl cons (p, q) fns id


// pg. 149
// ------------------------------------------------------------------------- //
// Overall Skolemization function.                                           //
// ------------------------------------------------------------------------- //

let askolemize fm =
    functions fm
    |> List.map fst
    |> skolem (simplify fm |> nnf)
    |> fst

let rec specialize fm =
    match fm with
    | Forall (x, p) ->
        specialize p
    | _ -> fm

let skolemize fm =
    askolemize fm
    |> pnf
    |> specialize
