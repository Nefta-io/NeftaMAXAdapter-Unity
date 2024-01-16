//
//  ALNeftaAdapter.h
//  NeftaMaxAdapter
//
//  Created by Tomaz Treven on 09/11/2023.
//

#ifndef ALNeftaAdapter_h
#define ALNeftaAdapter_h

#import <AppLovinSDK/AppLovinSDK.h>

#import <NeftaSDK/NeftaSDK-Swift.h>

@interface ALNeftaMediationAdapter : ALMediationAdapter <MAAdViewAdapter, MAInterstitialAdapter, MARewardedAdapter>

+ (void)ApplyRenderer:(id<MAAdapterResponseParameters>)parameters;
@end

#endif /* ALNeftaAdapter_h */
