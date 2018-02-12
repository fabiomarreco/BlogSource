---
layout: post
title: "Persistence Model != Domain Model"
date: 2018-01-12 23:57:52
comments: true
keywords: "C#, domain driven design, ddd, specification, visitor, EntityFramework"
description: "A short (yet opinionated) post about relationships between ORM and the Domain Model."
categories: Design Patterns
tags:
- Design Pattern
- EntityFramework
- Visitor
- Specification
- C#
---

#Persistence Model != Domain Model

I should start ths post explaining my motivations which might not be be clear to everyone. Probably a lot of people will (strongly) disagree with me.

When I worked with an ORM for the first time (EntityFramework/CodeFirst) it felt very liberating. Not worrying about how data was persisted, the relationships between tables, types and a clever sql schema migration freed my time to actually think on the requirements and the domain model.

At first.

As the project started to grow, requirements became more complicated, performance started to become an issue, and the database became a bottleneck once again. That was when we decided to use Database First strategy with EF. It worked for quite a while. However, eventually we noticed we were spending way too much time on attributes, mapping field relations, etc. (less then using ADO, that´s for sure, but still..). 

The problem is we wanted our domain objects to be/behave a certain way and tweaking EF was time consuming, if not impossible. We were struggling with simple requirements such as:
- We wanted to use [Value Object](https://martinfowler.com/bliki/ValueObject.html) to represent simple properties.
- Have an immutable list on the Entity (only accessible with an `Add(..)` for example)
- Not allow an empty constructor *(I know this has been resolved in newer versions.)*

We never actually fixed these problems, unfortunetly, the project ended before we could address these problems. But the experience helped me form an opinion on how to better use an ORM.  

Now I believe that we should have a Domain Model (residing on the *inner* domain layer) and a Persistence Model (residing on the *outer* infrastructure layer). These models should be completely independent from each other. Their relationship should only be through the `Repository` that should be responsible for translating between these models.

This is not a new idea, others have proposed this before. But I just wanted to share my opinion to justify the reason behind other posts i´ve been doing.

I am also aware that this strategy will increase the code base, and perhaps complexity. I´m not stating this is how it should always be done. Using a single model in a smaller project is fine. Using a single model if your database schema is very simple and you dont have many invariants to adhere is also fine.

If you choose to keep separate domains however, not only your domain model will be more expressive, but your persistence will benefit from it too:
- More control over your queries without necessarily relying on navigation properties to assemble your entity. 
- Less time fighting ORM to map your class model. If the mapping is complex, just leave as it is on the database, and fix it on the translation.

At the end of the day, it´s up to the developer to choose what is best for each project. You could start small, using the same model and then change it when necessary.

