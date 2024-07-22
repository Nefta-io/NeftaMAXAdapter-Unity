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
@property(nonatomic, strong) NSString *placementId;
@property(nonatomic) int state;
@property(nonatomic, strong) id<MAAdapterDelegate> listener;
- (void)Load:(id<MAAdapterResponseParameters>)parameters andNotify:(id<MAAdapterDelegate>)delegate;
@end

#endif /* ALNeftaAdapter_h */
