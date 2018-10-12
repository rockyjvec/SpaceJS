console.log("Testing SpaceJS...");
;;;;
// test
/* test */
function assert(expr)
{
  if(!expr)
  {
    console.log("Failed!");

    throw "Failed!";
  }
}

var t = "test";
assert(t == "test");

var asdf = {test: 0, test1: 1};
asdf.test = 10;
assert(asdf.test == 10 && asdf['test1'] == 1);

var x = 0;
assert(++x == 1);
assert(x == 1);
assert(x++ == 1);
assert(x == 2);
var loop = "";
for(var i = 0; i < 22;i++)
{
  loop += i;
  if(i > 5)
    i+= 20;
}
assert(loop == "0123456");
assert(i == 27);

loop = "";
var k = i;
while(i > 0)
{
  loop += i;
  i = i - 1;
  if(i == 5) break;
  if(i < 10) continue;
  k = i;
}
assert(i == 5);
assert(loop == "2726252423222120191817161514131211109876");
assert(k == 10);
while(i > 0) i--;
assert(i == 0);

do
{
  i++;
}
while(i < 2);
assert(i == 2);

function test(v)
{
    return v;
}

assert(test("asdf") == "asdf");

loop = "";
i = 0;
do{
    k = test("" + ++k);
    loop += k;
    i++;
}
while(i < 2);

assert(loop == "1112");
assert(k == "12");

console.log("Pass!");
