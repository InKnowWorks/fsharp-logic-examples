﻿// ========================================================================= //
// Copyright (c) 2003-2007, John Harrison.                                   //
// Copyright (c) 2012 Eric Taucher, Jack Pappas, Anh-Dung Phan               //
// (See "LICENSE.txt" for details.)                                          //
// ========================================================================= //

#load "initialization.fsx"

open FSharpx.Books.AutomatedReasoning.formulas
open FSharpx.Books.AutomatedReasoning.prop
open FSharpx.Books.AutomatedReasoning.fol
open FSharpx.Books.AutomatedReasoning.skolem
open FSharpx.Books.AutomatedReasoning.meson
open FSharpx.Books.AutomatedReasoning.interpolation

fsi.AddPrinter sprint_fol_formula

// pg. 429
// ------------------------------------------------------------------------- //
// Example.                                                                  //
// ------------------------------------------------------------------------- //

let p002 = prenex (parse @"(forall x. R(x,f(x))) /\ (forall x y. S(x,y) <=> R(x,y) \/ R(y,x))");;

let q002 = prenex (parse @"(forall x y z. S(x,y) /\ S(y,z) ==> T(x,z)) /\ ~T(0,0)");;

let c002 = urinterpolate p002 q002;;

meson002(Imp(p002,c002));;

meson002(Imp(q002,Not c002));;
        
// pg. 433
// ------------------------------------------------------------------------- //
// The same example now gives a true interpolant.                            //
// ------------------------------------------------------------------------- //

let c003 = uinterpolate p002 q002;;

meson002(Imp(p002,c003));;

meson002(Imp(q002,Not c003));;
        
// pg. 434
// ------------------------------------------------------------------------- //
// Example.                                                                  //
// ------------------------------------------------------------------------- //

let p004 = (parse @"(forall x. exists y. R(x,y)) /\ (forall x y. S(v,x,y) <=> R(x,y) \/ R(y,x))");;

let q004 = (parse @"(forall x y z. S(v,x,y) /\ S(v,y,z) ==> T(x,z)) /\ (exists u. ~T(u,u))");;

let c004 = interpolate p004 q004;;

meson002(Imp(p004,c004));;

meson002(Imp(q004,Not c004));;

// ------------------------------------------------------------------------- //
// More examples, not in the text.                                           //
// ------------------------------------------------------------------------- //

let p005 = (parse @"(p ==> q /\ r)");;

let q005 = (parse @"~((q ==> p) ==> s ==> (p <=> q))");;

let c005 = interpolate p005 q005;;

tautology(Imp(And(p005,q005),False));;

tautology(Imp(p005,c005));;

tautology(Imp(q005,Not c005));;

// ------------------------------------------------------------------------- //
// A more interesting example.                                               //
// ------------------------------------------------------------------------- //

let p006 = (parse @"(forall x. exists y. R(x,y)) /\ (forall x y. S(x,y) <=> R(x,y) \/ R(y,x))");;

let q006 = (parse @"(forall x y z. S(x,y) /\ S(y,z) ==> T(x,z)) /\ ~T(u,u)");;

meson002(Imp(And(p006,q006),False));;

let c006 = interpolate p006 q006;;

meson002(Imp(p006,c006));;

meson002(Imp(q006,Not c006));;

// ------------------------------------------------------------------------- //
// A variant where u is free in both parts.                                  //
// ------------------------------------------------------------------------- //

let p007 = (parse @"(forall x. exists y. R(x,y)) /\ (forall x y. S(x,y) <=> R(x,y) \/ R(y,x)) /\ (forall v. R(u,v) ==> Q(v,u))");;

let q007 = (parse @"(forall x y z. S(x,y) /\ S(y,z) ==> T(x,z)) /\ ~T(u,u)");;

meson002(Imp(And(p007,q007),False));;

let c007 = interpolate p007 q007;;

meson002(Imp(p007,c007));;

meson002(Imp(q007,Not c007));;

// ------------------------------------------------------------------------- //
// Way of generating examples quite easily (see K&K exercises).              //
// ------------------------------------------------------------------------- //

let test_interp fm =
    let rec p = generalize (skolemize fm)
    and q = generalize (skolemize (Not fm))
    let c = interpolate p q
    meson002(Imp(And(p,q),False)) |> ignore
    meson002(Imp(p,c)) |> ignore
    meson002(Imp(q,Not c)) |> ignore
    c;;
    
test_interp (parse @"forall x. P(x) ==> exists y. forall z. P(z) ==> Q(y)");;

test_interp (parse @"forall y. exists y. forall z. exists a. P(a,x,y,z) ==> P(x,y,z,a)");;

// ------------------------------------------------------------------------- //
// Hintikka's examples.                                                      //
// ------------------------------------------------------------------------- //

let p009 = (parse @"forall x. L(x,b)");;

let q009 = (parse @"(forall y. L(b,y) ==> m = y) /\ ~(m = b)");;

let c009 = einterpolate p009 q009;;

meson002(Imp(p009,c009));;

meson002(Imp(q009,Not c009));;

let p010 = (parse @"(forall x. A(x) /\ C(x) ==> B(x)) /\ (forall x. D(x) \/ ~D(x) ==> C(x))");;

let q010 = (parse @"~(forall x. E(x) ==> A(x) ==> B(x))");;

let c010 = interpolate p010 q010;;

meson002(Imp(p010,c010));;

meson002(Imp(q010,Not c010));;
