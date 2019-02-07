
using System;

namespace aflang {



public class Parser {
	public const int _EOF = 0;
	public const int _ident = 1;
	public const int _number = 2;
	public const int maxT = 28;

	const bool _T = true;
	const bool _x = false;
	const int minErrDist = 2;
	
	public Scanner scanner;
	public Errors  errors;

	public Token t;    // last recognized token
	public Token la;   // lookahead token
	int errDist = minErrDist;

const int // types
	  undef = 0, integer = 1, boolean = 2;

	const int // object kinds
	  var = 0, proc = 1;

	public SymbolTable   tab;
	public CodeGenerator gen;
  public NodeFactory factory;
  
/*--------------------------------------------------------------------------*/


	public Parser(Scanner scanner) {
		this.scanner = scanner;
		errors = new Errors();
	}

	void SynErr (int n) {
		if (errDist >= minErrDist) errors.SynErr(la.line, la.col, n);
		errDist = 0;
	}

	public void SemErr (string msg) {
		if (errDist >= minErrDist) errors.SemErr(t.line, t.col, msg);
		errDist = 0;
	}
	
	void Get () {
		for (;;) {
			t = la;
			la = scanner.Scan();
			if (la.kind <= maxT) { ++errDist; break; }

			la = t;
		}
	}
	
	void Expect (int n) {
		if (la.kind==n) Get(); else { SynErr(n); }
	}
	
	bool StartOf (int s) {
		return set[s, la.kind];
	}
	
	void ExpectWeak (int n, int follow) {
		if (la.kind == n) Get();
		else {
			SynErr(n);
			while (!StartOf(follow)) Get();
		}
	}


	bool WeakSeparator(int n, int syFol, int repFol) {
		int kind = la.kind;
		if (kind == n) {Get(); return true;}
		else if (StartOf(repFol)) {return false;}
		else {
			SynErr(n);
			while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
				Get();
				kind = la.kind;
			}
			return StartOf(syFol);
		}
	}

	
	void aflang() {
		string name; Root root; 
		Expect(3);
		Ident(out name);
		tab.OpenScope(); root = new Root(); 
		Expect(4);
		while (la.kind == 6 || la.kind == 26 || la.kind == 27) {
			if (la.kind == 26 || la.kind == 27) {
				VarDecl();
			} else {
				ProcDecl();
			}
		}
		Expect(5);
		tab.CloseScope(); if (!gen.MainDefined) SemErr("main function never defined"); 
	}

	void Ident(out string name) {
		Expect(1);
		name = t.val; 
	}

	void VarDecl() {
		string name; int type; 
		Type(out type);
		Ident(out name);
		tab.NewObj(name, var, type); 
		while (la.kind == 9) {
			Get();
			Ident(out name);
			tab.NewObj(name, var, type); 
		}
		Expect(10);
	}

	void ProcDecl() {
		string name; Obj obj; INode fun, t1; 
		Expect(6);
		Ident(out name);
		obj = tab.NewObj(name, proc, undef); tab.OpenScope(); 
		Expect(7);
		Expect(8);
		Expect(4);
		while (StartOf(1)) {
			if (la.kind == 26 || la.kind == 27) {
				VarDecl();
			} else {
				Stat(out t1);
			}
		}
		Expect(5);
		tab.CloseScope(); 
	}

	void Stat(out INode n) {
		int type; string name; Obj obj; loopstart; INode t1, t2; n = new Error();
		if (la.kind == 1) {
			Ident(out name);
			obj = tab.Find(name);
			if (la.kind == 11) {
				Get();
				if (obj.kind != var) SemErr("cannot assign to procedure"); 
				Expr(out type, out INode expr);
				Expect(10);
				if (type != obj.type) SemErr("incompatible types");
				if (obj.level == 0) gen.Emit(Op.STOG, obj.adr);
				else gen.Emit(Op.STO, obj.adr); 
			} else if (la.kind == 7) {
				Get();
				Expect(8);
				Expect(10);
				if (obj.kind != proc) SemErr("object is not a procedure"); n = new Call(name); 
			} else SynErr(29);
		} else if (la.kind == 12) {
			Get();
			Expect(7);
			Expr(out type, out INode expr);
			Expect(8);
			if (type != boolean) SemErr("boolean type expected");
			Stat(out t1);
			if (la.kind == 13) {
				Get();
				Stat(out t2);
			}
			
		} else if (la.kind == 14) {
			Get();
			loopstart = gen.pc; 
			Expect(7);
			Expr(out type, out INode expr);
			Expect(8);
			if (type != boolean) SemErr("boolean type expected"); adr = gen.pc - 2; 
			Stat(out t1);
		} else if (la.kind == 15) {
			Get();
			Ident(out name);
			Expect(10);
			obj = tab.Find(name);
			if (obj.type != integer) SemErr("integer type expected");
			gen.Emit(Op.READ);
			if (obj.level == 0) gen.Emit(Op.STOG, obj.adr);
			else gen.Emit(Op.STO, obj.adr); 
		} else if (la.kind == 16) {
			Get();
			Expr(out type, out INode expr);
			Expect(10);
			if (type != integer) SemErr("integer type expected");
		} else SynErr(30);
	}

	void Type(out int type) {
		type = undef; 
		if (la.kind == 26) {
			Get();
			type = integer; 
		} else if (la.kind == 27) {
			Get();
			type = boolean; 
		} else SynErr(31);
	}

	void Expr(out int type, out INode expr) {
		SimExpr(out type, out INode left);
		if (la.kind == 23 || la.kind == 24 || la.kind == 25) {
			RelOp(out Op op);
			SimExpr(out int type1, out INode temp);
			if (type != type1) SemErr("incompatible types"); type = boolean; 
		}
	}

	void SimExpr(out int type, out INode node) {
		temp; 
		Term(out type, out INode left);
		node = left; 
		while (la.kind == 17 || la.kind == 20) {
			AddOp(out Op op);
			temp = node; node = factory.Create(op.ToString()); 
			Term(out int type1, out INode right);
			if (type != integer || type1 != integer) SemErr("integer type expected"); 
			node.Childrens.Add(temp); node.Childrens.Add(right); 
		}
	}

	void RelOp(out Op op) {
		op = Op.EQU; 
		if (la.kind == 23) {
			Get();
		} else if (la.kind == 24) {
			Get();
			op = Op.LSS; 
		} else if (la.kind == 25) {
			Get();
			op = Op.GTR; 
		} else SynErr(32);
	}

	void Term(out int type, out INode n) {
		Factor(out type, out INode left);
		n = left; 
		while (la.kind == 21 || la.kind == 22) {
			MulOp(out Op op);
			n = factory.Create(op);  
			Factor(out int type1, out INode right);
			if (type != integer || type1 != integer) SemErr("integer type expected"); 
		}
	}

	void AddOp(out Op op) {
		op = Op.ADD; 
		if (la.kind == 20) {
			Get();
		} else if (la.kind == 17) {
			Get();
			op = Op.SUB; 
		} else SynErr(33);
	}

	void Factor(out int type, out INode node) {
		int n; Obj obj; string name; node = new Error(); 
		type = undef; 
		if (la.kind == 1) {
			Ident(out name);
			obj = tab.Find(name); type = obj.type;
			if (obj.kind == var) {
			 if (obj.level == 0) {
			   node = factory.Create("gvar");
			   node.Value = obj.adr.ToString(); 
			 } else {
			   node = factory.Create("gvar");
			   node.Value = obj.adr.ToString(); 
			 } 
			} else SemErr("variable expected"); 
		} else if (la.kind == 2) {
			Get();
			n = int.Parse(t.val); node = factory.Create("const"); 
			node.Value = t.val; type = integer; 
		} else if (la.kind == 17) {
			Get();
			Factor(out type, out node);
			if (type != integer) {
			SemErr("integer type expected"); type = integer;
			}
			node.Value = '-' + node.Value; 
		} else if (la.kind == 18) {
			Get();
			node = new Bool(); node.Value = "true"; type = boolean; 
		} else if (la.kind == 19) {
			Get();
			node = new Bool(); node.Value = "false"; type = boolean; 
		} else SynErr(34);
	}

	void MulOp(out Op op) {
		op = Op.MUL; 
		if (la.kind == 21) {
			Get();
		} else if (la.kind == 22) {
			Get();
			op = Op.DIV; 
		} else SynErr(35);
	}



	public void Parse() {
		la = new Token();
		la.val = "";		
		Get();
		aflang();
		Expect(0);

	}
	
	static readonly bool[,] set = {
		{_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x},
		{_x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_x,_T,_T, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_T, _x,_x}

	};
} // end Parser


public class Errors {
	public int count = 0;                                    // number of errors detected
	public System.IO.TextWriter errorStream = Console.Out;   // error messages go to this stream
	public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text

	public virtual void SynErr (int line, int col, int n) {
		string s;
		switch (n) {
			case 0: s = "EOF expected"; break;
			case 1: s = "ident expected"; break;
			case 2: s = "number expected"; break;
			case 3: s = "\"program\" expected"; break;
			case 4: s = "\"{\" expected"; break;
			case 5: s = "\"}\" expected"; break;
			case 6: s = "\"void\" expected"; break;
			case 7: s = "\"(\" expected"; break;
			case 8: s = "\")\" expected"; break;
			case 9: s = "\",\" expected"; break;
			case 10: s = "\";\" expected"; break;
			case 11: s = "\"=\" expected"; break;
			case 12: s = "\"if\" expected"; break;
			case 13: s = "\"else\" expected"; break;
			case 14: s = "\"while\" expected"; break;
			case 15: s = "\"read\" expected"; break;
			case 16: s = "\"write\" expected"; break;
			case 17: s = "\"-\" expected"; break;
			case 18: s = "\"true\" expected"; break;
			case 19: s = "\"false\" expected"; break;
			case 20: s = "\"+\" expected"; break;
			case 21: s = "\"*\" expected"; break;
			case 22: s = "\"/\" expected"; break;
			case 23: s = "\"==\" expected"; break;
			case 24: s = "\"<\" expected"; break;
			case 25: s = "\">\" expected"; break;
			case 26: s = "\"int\" expected"; break;
			case 27: s = "\"bool\" expected"; break;
			case 28: s = "??? expected"; break;
			case 29: s = "invalid Stat"; break;
			case 30: s = "invalid Stat"; break;
			case 31: s = "invalid Type"; break;
			case 32: s = "invalid RelOp"; break;
			case 33: s = "invalid AddOp"; break;
			case 34: s = "invalid Factor"; break;
			case 35: s = "invalid MulOp"; break;

			default: s = "error " + n; break;
		}
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}

	public virtual void SemErr (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}
	
	public virtual void SemErr (string s) {
		errorStream.WriteLine(s);
		count++;
	}
	
	public virtual void Warning (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
	}
	
	public virtual void Warning(string s) {
		errorStream.WriteLine(s);
	}
} // Errors


public class FatalError: Exception {
	public FatalError(string m): base(m) {}
}
}