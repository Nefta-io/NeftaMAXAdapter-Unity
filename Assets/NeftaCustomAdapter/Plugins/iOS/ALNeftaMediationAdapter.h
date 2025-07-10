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
typedef NS_ENUM(NSInteger, AdType) {
    AdTypeOther = 0,
    AdTypeBanner = 1,
    AdTypeInterstitial = 2,
    AdTypeRewarded = 3
};

+ (void)OnExternalMediationRequestLoad:(AdType)adType ad:(MAAd * _Nonnull)ad usedInsight:(AdInsight * _Nullable)usedInsight;
+ (void)OnExternalMediationRequestFail:(AdType)adType adUnitIdentifier:(NSString* _Nonnull)adUnitIdentifier usedInsight:(AdInsight * _Nullable)usedInsight error:(MAError * _Nonnull)error;
+ (void)OnExternalMediationImpression:(MAAd* _Nonnull)ad;
+ (void)OnExternalMediationImpressionAsString:(NSString* _Nonnull)network format:(NSString * _Nonnull)format creativeId:(NSString * _Nullable)creativeId data:(NSString * _Nonnull)data revenue:(double)revenue precision:(NSString * _Nonnull)precision;
@property ALNeftaAd * _Nullable ad;
@end

#endif /* ALNeftaAdapter_h */
