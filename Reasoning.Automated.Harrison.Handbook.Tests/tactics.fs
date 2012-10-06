﻿// ========================================================================= //
// Copyright (c) 2003-2007, John Harrison.                                   //
// Copyright (c) 2012 Eric Taucher, Jack Pappas, Anh-Dung Phan               //
// (See "LICENSE.txt" for details.)                                          //
// ========================================================================= //

module Reasoning.Automated.Harrison.Handbook.Tests.tactics

open Reasoning.Automated.Harrison.Handbook.lib
open Reasoning.Automated.Harrison.Handbook.formulas
open Reasoning.Automated.Harrison.Handbook.folMod
open Reasoning.Automated.Harrison.Handbook.lcf
open Reasoning.Automated.Harrison.Handbook.lcfprop
open Reasoning.Automated.Harrison.Handbook.folderived
open Reasoning.Automated.Harrison.Handbook.tactics
open NUnit.Framework
open FsUnit

// pg. 514
// ------------------------------------------------------------------------- //
// A simple example.                                                         //
// ------------------------------------------------------------------------- //

[<Test>]
let ``test goal``() = 
    let g0 = set_goal (parse "(forall x. x <= x) /\ (forall x y z. x <= y /\ y <= z ==> x <= z) /\ (forall x y. f(x) <= y <=> x <= g(y)) ==> (forall x y. x <= y ==> f(x) <= f(y)) /\ (forall x y. x <= y ==> g(x) <= g(y))")
    let g1 = imp_intro_tac "ant" g0
    let g2 = conj_intro_tac g1
    let g3 = funpow 2 (auto_tac by ["ant"]) g2
    extract_thm g3
    |> sprint_thm
    |> should equal "|- (forall x. x <=x) /\ (forall x y z. x <=y /\ y <=z ==> x <=z) /\ (forall x y. f(x) <=y <=> x <=g(y)) ==> (forall x y. x <=y ==> f(x) <=f(y)) /\ (forall x y. x <=y ==> g(x) <=g(y))"
    
// pg. 514
// ------------------------------------------------------------------------- //
// All packaged up together.                                                 //
// ------------------------------------------------------------------------- //

[<Test>]
let ``test prove tactics 1``() = 
    prove (parse "(forall x. x <= x) /\(forall x y z. x <= y /\ y <= z ==> x <= z) /\(forall x y. f(x) <= y <=> x <= g(y))==> (forall x y. x <= y ==> f(x) <= f(y)) /\ (forall x y. x <= y ==> g(x) <= g(y))")
            [imp_intro_tac "ant";
            conj_intro_tac;
            auto_tac by ["ant"];
            auto_tac by ["ant"]] 
    |> sprint_thm
    |> should equal "|- (forall x. x <=x) /\ (forall x y z. x <=y /\ y <=z ==> x <=z) /\ (forall x y. f(x) <=y <=> x <=g(y)) ==> (forall x y. x <=y ==> f(x) <=f(y)) /\ (forall x y. x <=y ==> g(x) <=g(y))"
      
// pg. 518
// ------------------------------------------------------------------------- //
// A simple example.                                                         //
// ------------------------------------------------------------------------- //

[<Test>]
let ``test prove tactics 2``() = 
    prove (parse "(forall x y. x <= y <=> x * y = x) /\ (forall x y. f(x * y) = f(x) * f(y)) ==> forall x y. x <= y ==> f(x) <= f(y)") [note("eq_sym",(parse "forall x y. x = y ==> y = x"))
    using [eq_sym (parset "x") (parset "y")];
    note("eq_trans",(parse "forall x y z. x = y /\ y = z ==> x = z"))
    using [eq_trans (parset "x") (parset "y") (parset "z")];
    note("eq_cong",(parse "forall x y. x = y ==> f(x) = f(y)"))
    using [axiom_funcong "f" [(parset "x")] [(parset "y")]];
    assume ["le",(parse "forall x y. x <= y <=> x * y = x");
            "hom",(parse "forall x y. f(x * y) = f(x) * f(y)")];
    fix "x"; fix "y";
    assume ["xy",(parse "x <= y")];
    so have (parse "x * y = x") by ["le"];
    so have (parse "f(x * y) = f(x)") by ["eq_cong"];
    so have (parse "f(x) = f(x * y)") by ["eq_sym"];
    so have (parse "f(x) = f(x) * f(y)") by ["eq_trans"; "hom"];
    so have (parse "f(x) * f(y) = f(x)") by ["eq_sym"];
    so conclude (parse "f(x) <= f(y)") by ["le"];
    qed] 
    |> sprint_thm
    |> should equal "|- (forall x y. x <=y <=> x *y =x) /\ (forall x y. f(x *y) =f(x) *f(y)) ==> (forall x y. x <=y ==> f(x) <=f(y))"

// ------------------------------------------------------------------------- //
// More examples not in the main text.                                       //
// ------------------------------------------------------------------------- //

[<Test>]
let ``test prove tactics 3``() = 
    prove
        (parse "(exists x. p(x)) ==> (forall x. p(x) ==> p(f(x))) ==> exists y. p(f(f(f(f(y)))))")
        [assume ["A",(parse "exists x. p(x)")];
        assume ["B",(parse "forall x. p(x) ==> p(f(x))")];
        note ("C",(parse "forall x. p(x) ==> p(f(f(f(f(x)))))"))
        proof
        [have (parse "forall x. p(x) ==> p(f(f(x)))") by ["B"];
            so conclude (parse "forall x. p(x) ==> p(f(f(f(f(x)))))") at once;
            qed];
        consider ("a",(parse "p(a)")) by ["A"];
        take (parset "a");
        so conclude (parse "p(f(f(f(f(a)))))") by ["C"];
        qed] 
    |> sprint_thm
    |> should equal "|- (exists x. p(x)) ==> (forall x. p(x) ==> p(f(x))) ==> (exists y. p(f(f(f(f(y))))))"

// ------------------------------------------------------------------------- //
// Alternative formulation with lemma construct.                             //
// ------------------------------------------------------------------------- //

[<Test>]
let ``test prove using lemma``() = 
    let lemma (s,p) = function
        | (Goals((asl,w)::gls,jfn) as gl) ->
            Goals((asl,p)::((s,p)::asl,w)::gls,
                function (thp::thw::oths) ->
                            jfn(imp_unduplicate(imp_trans thp (shunt thw)) :: oths)
                       | _ -> failwith "malform input")
        | _ -> failwith "malform lemma"
    prove
        (parse "(exists x. p(x)) ==> (forall x. p(x) ==> p(f(x))) ==> exists y. p(f(f(f(f(y)))))")
        [assume ["A",(parse "exists x. p(x)")];
        assume ["B",(parse "forall x. p(x) ==> p(f(x))")];
        lemma ("C",(parse "forall x. p(x) ==> p(f(f(f(f(x)))))"));
            have (parse "forall x. p(x) ==> p(f(f(x)))") by ["B"];
            so conclude (parse "forall x. p(x) ==> p(f(f(f(f(x)))))") at once;
            qed;
        consider ("a",(parse "p(a)")) by ["A"];
        take (parset "a");
        so conclude (parse "p(f(f(f(f(a)))))") by ["C"];
        qed] 
    |> sprint_thm
    |> should equal "|- (exists x. p(x)) ==> (forall x. p(x) ==> p(f(x))) ==> (exists y. p(f(f(f(f(y))))))"

// ------------------------------------------------------------------------- //
// Examples.                                                                 //
// ------------------------------------------------------------------------- //

[<Test>]
let ``test prove tactics 4``() = 
    prove (parse "p(a) ==> (forall x. p(x) ==> p(f(x))) ==> exists y. p(y) /\ p(f(y))")
            [our thesis at once;
            qed] 
    |> sprint_thm
    |> should equal "|- p(a) ==> (forall x. p(x) ==> p(f(x))) ==> (exists y. p(y) /\ p(f(y)))"

[<Test>]
let ``test prove tactics 5``() = 
    prove
        (parse "(exists x. p(x)) ==> (forall x. p(x) ==> p(f(x))) ==> exists y. p(f(f(f(f(y)))))")
        [assume ["A",(parse "exists x. p(x)")];
        assume ["B",(parse "forall x. p(x) ==> p(f(x))")];
        note ("C",(parse "forall x. p(x) ==> p(f(f(f(f(x)))))")) proof
        [have (parse "forall x. p(x) ==> p(f(f(x)))") by ["B"];
            so our thesis at once;
            qed];
        consider ("a",(parse "p(a)")) by ["A"];
        take (parset "a");
        so our thesis by ["C"];
        qed] 
    |> sprint_thm
    |> should equal "|- (exists x. p(x)) ==> (forall x. p(x) ==> p(f(x))) ==> (exists y. p(f(f(f(f(y))))))"

[<Test>]
let ``test prove tactics 6``() = 
    prove (parse "forall a. p(a) ==> (forall x. p(x) ==> p(f(x))) ==> exists y. p(y) /\ p(f(y))")
            [fix "c";
            assume ["A",(parse "p(c)")];
            assume ["B",(parse "forall x. p(x) ==> p(f(x))")];
            take (parset "c");
            conclude (parse "p(c)") by ["A"];
            note ("C",(parse "p(c) ==> p(f(c))")) by ["B"];
            so our thesis by ["C"; "A"];
            qed] 
    |> sprint_thm
    |> should equal "|- forall a. p(a) ==> (forall x. p(x) ==> p(f(x))) ==> (exists y. p(y) /\ p(f(y)))"

[<Test>]
let ``test prove tactics 7``() = 
    prove (parse "p(c) ==> (forall x. p(x) ==> p(f(x))) ==> exists y. p(y) /\ p(f(y))")
            [assume ["A",(parse "p(c)")];
            assume ["B",(parse "forall x. p(x) ==> p(f(x))")];
            take (parset "c");
            conclude (parse "p(c)") by ["A"];
            our thesis by ["A"; "B"];
            qed] 
    |> sprint_thm
    |> should equal "|- p(c) ==> (forall x. p(x) ==> p(f(x))) ==> (exists y. p(y) /\ p(f(y)))"

[<Test>]
let ``test prove tactics 8``() = 
    prove (parse "forall a. p(a) ==> (forall x. p(x) ==> p(f(x))) ==> exists y. p(y) /\ p(f(y))")
            [fix "c";
            assume ["A",(parse "p(c)")];
            assume ["B",(parse "forall x. p(x) ==> p(f(x))")];
            take (parset "c");
            conclude (parse "p(c)") by ["A"];
            note ("C",(parse "p(c) ==> p(f(c))")) by ["B"];
            our thesis by ["C"; "A"];
            qed] 
    |> sprint_thm
    |> should equal "|- forall a. p(a) ==> (forall x. p(x) ==> p(f(x))) ==> (exists y. p(y) /\ p(f(y)))"

[<Test>]
let ``test prove tactics 9``() = 
    prove (parse "forall a. p(a) ==> (forall x. p(x) ==> p(f(x))) ==> exists y. p(y) /\ p(f(y))")
            [fix "c";
            assume ["A",(parse "p(c)")];
            assume ["B",(parse "forall x. p(x) ==> p(f(x))")];
            take (parset "c");
            note ("D",(parse "p(c)")) by ["A"];
            note ("C",(parse "p(c) ==> p(f(c))")) by ["B"];
            our thesis by ["C"; "A"; "D"];
            qed] 
    |> sprint_thm
    |> should equal "|- forall a. p(a) ==> (forall x. p(x) ==> p(f(x))) ==> (exists y. p(y) /\ p(f(y)))" 

[<Test>]
let ``test prove tactics 10``() = 
    prove (parse "(p(a) \/ p(b)) ==> q ==> exists y. p(y)")
        [assume ["A",(parse "p(a) \/ p(b)")];
        assume ["",(parse "q")];
        cases (parse "p(a) \/ p(b)") by ["A"];
            take (parset "a");
            so our thesis at once;
            qed;
            take (parset "b");
            so our thesis at once;
            qed] 
    |> sprint_thm
    |> should equal "|- p(a) \/ p(b) ==> q ==> (exists y. p(y))"
        
[<Test>]
let ``test prove tactics 11``() = 
    let v1 = "A"
    let v2 = (parse "p(a)")
    prove
        (parse "(p(a) \/ p(b)) /\ (forall x. p(x) ==> p(f(x))) ==> exists y. p(f(y))")
        [assume ["base",(parse "p(a) \/ p(b)");
                "Step",(parse "forall x. p(x) ==> p(f(x))")];
        cases (parse "p(a) \/ p(b)") by ["base"]; 
            so note (v1, v2) at once; // use function app instead of value
            note ("X",(parse "p(a) ==> p(f(a))")) by ["Step"];
            take (parset "a");
            our thesis by ["A"; "X"];
            qed;
            take (parset "b");
            so our thesis by ["Step"];
            qed] 
    |> sprint_thm
    |> should equal "|- (p(a) \/ p(b)) /\ (forall x. p(x) ==> p(f(x))) ==> (exists y. p(f(y)))"

[<Test>]
let ``test prove tactics 12``() = 
    prove
        (parse "(exists x. p(x)) ==> (forall x. p(x) ==> p(f(x))) ==> exists y. p(f(y))")
        [assume ["A",(parse "exists x. p(x)")];
        assume ["B",(parse "forall x. p(x) ==> p(f(x))")];
        consider ("a",(parse "p(a)")) by ["A"];
        so note ("concl",(parse "p(f(a))")) by ["B"];
        take (parset "a");
        our thesis by ["concl"];
        qed] 
    |> sprint_thm
    |> should equal "|- (exists x. p(x)) ==> (forall x. p(x) ==> p(f(x))) ==> (exists y. p(f(y)))"

[<Test>]
let ``test prove tactics 13``() = 
    prove (parse "(forall x. p(x) ==> q(x)) ==> (forall x. q(x) ==> p(x))
            ==> (p(a) <=> q(a))")
        [assume ["A",(parse "forall x. p(x) ==> q(x)")];
        assume ["B",(parse "forall x. q(x) ==> p(x)")];
        note ("von",(parse "p(a) ==> q(a)")) by ["A"];
        note ("bis",(parse "q(a) ==> p(a)")) by ["B"];
        our thesis by ["von"; "bis"];
        qed] 
    |> sprint_thm
    |> should equal "|- (forall x. p(x) ==> q(x)) ==> (forall x. q(x) ==> p(x)) ==> (p(a) <=> q(a))"