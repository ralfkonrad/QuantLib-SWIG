/*
 Copyright (C) 2015 Gouthaman Balaraman
 Copyright (C) 2018 Matthias Lungwitz

 This file is part of QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/

 QuantLib is free software: you can redistribute it and/or modify it
 under the terms of the QuantLib license.  You should have received a
 copy of the license along with this program; if not, please email
 <quantlib-dev@lists.sf.net>. The license is also available online at
 <http://quantlib.org/license.shtml>.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/


#ifndef quantlib_forward_i
#define quantlib_forward_i

%include instruments.i
%include termstructures.i
%include interestrate.i

// Forward
%{
using QuantLib::Forward;
%}

%shared_ptr(Forward)
class Forward : public Instrument{
    public:
        Date settlementDate() const;
        bool isExpired() const;
        const Calendar& calendar() const;
        BusinessDayConvention businessDayConvention() const;
        const DayCounter& dayCounter() const;
        Handle<YieldTermStructure> discountCurve() const;
        Handle<YieldTermStructure> incomeDiscountCurve() const;
        Real spotValue() const;
        Real spotIncome(const Handle<YieldTermStructure>&
                                               incomeDiscountCurve) const;

        // calculations
        Real forwardValue();
        InterestRate impliedYield(
                        Real underlyingSpotValue,
                        Real forwardValue,
                        Date settlementDate,
                        Compounding compoundingConvention,
                        DayCounter dayCounter);
    private:
        Forward();
};


%{
using QuantLib::BondForward;
using QuantLib::FixedRateBond;
using QuantLib::BusinessDayConvention;
using QuantLib::Position;
%}

%shared_ptr(BondForward)
class BondForward : public Forward {
  public:
    BondForward(const Date& valueDate,
                const Date& maturityDate,
                Position::Type type,
                Real strike,
                Natural settlementDays,
                const DayCounter& dayCounter,
                const Calendar& calendar,
                BusinessDayConvention businessDayConvention,
                const ext::shared_ptr<Bond>& bond,
                const Handle<YieldTermStructure>& discountCurve = {},
                const Handle<YieldTermStructure>& incomeDiscountCurve = {});

    Real forwardPrice();
    Real cleanForwardPrice();
};

#endif
