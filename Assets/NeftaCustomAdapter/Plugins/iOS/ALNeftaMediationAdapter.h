//
//  ALNeftaAdapter.h
//  NeftaMaxAdapter
//
//  Created by Tomaz Treven on 09/11/2023.
//

#ifndef ALNeftaAdapter_h
#define ALNeftaAdapter_h

#import "ALNeftaAd.h"

@interface ALNeftaMediationAdapter : ALMediationAdapter <MAAdViewAdapter, MAInterstitialAdapter, MARewardedAdapter>
@property ALNeftaAd * _Nullable ad;
@end

#endif /* ALNeftaAdapter_h */
