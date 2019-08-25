This is demo of multi threaded asynchronous calls to the service application
As a service, I use just simple library which return string with random delay
I will use various ways to add asynchronious tasks to demonstrate it
and compare with normal, synchronous task

So, I have 4 possible scenarious, synchronous is obviously the longest one 
because I'm getting slow data one after each other

Quite opposite is CallAsynchronously, when I'm getting all data in parallel

Most realistic scenario is async call in batches: CallAsynchronouslyBatches
It's because often we have some limitation of parallelism, 
like we can't call same web service more than 20 times at the same time.

Last one and the trickiest is with yield: TestYieldCalls
Basically I'm creating "lazy" dictionarly of tasks. 
And I had issue with scope, when was creating these demo. 
Practicality of it? Well.. 
Normally asynchronous tasks will come back unordered, shortest will come first.
And sometimes we need to order result in given order. 
Like list of named payments should be in the same order.
So this sever this purpose. 

TODO: write another version of TestYieldCalls with batches. I will do it later. 

heh... should I put copyright? Feel free to use it, 
Serge Klokov, 2019 :)
Enjoy!

