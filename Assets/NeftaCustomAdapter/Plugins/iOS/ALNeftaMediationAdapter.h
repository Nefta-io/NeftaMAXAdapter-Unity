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

+ (void)OnExternalMediationRequestLoad:(AdType)adType recommendedAdUnitId:(NSString* _Nullable)recommendedAdUnitId calculatedFloorPrice:(double)calculatedFloorPrice ad:(MAAd * _Nonnull)ad;
+ (void)OnExternalMediationRequestFail:(AdType)adType recommendedAdUnitId:(NSString* _Nullable)recommendedAdUnitId calculatedFloorPrice:(double)calculatedFloorPrice adUnitIdentifier:(NSString * _Nonnull)adUnitIdentifier error:(MAError * _Nonnull)error;

+ (void)OnExternalMediationRequestLoad:(AdType)adType requestedFloorPrice:(double)requestedFloorPrice calculatedFloorPrice:(double)calculatedFloorPrice ad:(MAAd * _Nonnull)ad;
+ (void)OnExternalMediationRequestFail:(AdType)adType requestedFloorPrice:(double)requestedFloorPrice calculatedFloorPrice:(double)calculatedFloorPrice adUnitIdentifier:(NSString * _Nonnull)adUnitIdentifier error:(MAError * _Nonnull)error;
+ (void)OnExternalMediationImpression:(MAAd* _Nonnull)ad;
+ (void)OnExternalMediationImpressionAsString:(NSString* _Nonnull)network format:(NSString * _Nonnull)format creativeId:(NSString * _Nullable)creativeId data:(NSString *)data;
@property ALNeftaAd * _Nullable ad;
@end

#endif /* ALNeftaAdapter_h */
