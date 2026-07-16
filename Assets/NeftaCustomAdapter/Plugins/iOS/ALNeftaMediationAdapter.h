//
//  ALNeftaMediationAdapter.h
//  ALNeftaMediationAdapter
//
//  Created by Tomaz Treven on 09/11/2023.
//

#ifndef ALNeftaMediationAdapter_h
#define ALNeftaMediationAdapter_h

#import <AppLovinSDK/AppLovinSDK.h>
#import <NeftaSDK/NeftaSDK-Swift.h>

@interface ALNeftaMediationAdapter : NSObject
typedef NS_ENUM(NSInteger, AdType) {
    AdTypeOther = 0,
    AdTypeBanner = 1,
    AdTypeInterstitial = 2,
    AdTypeRewarded = 3
};
+ (void)InitWithAppId:(NSString *_Nonnull)appId onReady:(void (^ _Nullable)(InitConfiguration * _Nonnull))onReady NS_SWIFT_NAME(Init(appId:onReady:));
+ (void)InitWithClientId:(NSString *_Nonnull)clientId onReady:(void (^ _Nullable)(InitConfiguration * _Nonnull))onReady NS_SWIFT_NAME(Init(clientId:onReady:));
+ (double)GetRetryDelayInSeconds:(AdInsight * _Nullable)insight adUnitId:(NSString * _Nonnull)adUnitId NS_SWIFT_NAME(GetRetryDelayInSeconds(insight:adUnitId:));

+ (void)GetInsights:(NSInteger)insights previousInsight:(AdInsight * _Nullable)previousInsight callback:(void (^ _Nullable)(Insights * _Nonnull))callback;
+ (void)GetInsightsBridge:(NSInteger)requestId insights:(NSInteger)insights previousRequestId:(NSInteger)previousRequestId;

+ (void)OnExternalMediationRequestWithBanner:(MAAdView * _Nonnull)banner insight:(AdInsight * _Nullable)insight;
+ (void)OnExternalMediationRequestWithBanner:(MAAdView * _Nonnull)banner;
+ (void)OnExternalMediationRequestWithBanner:(MAAdView * _Nonnull)banner customBidPrice:(double)customBidPrice;
+ (void)OnExternalMediationRequestWithInterstitial:(MAInterstitialAd * _Nonnull)interstitial insight:(AdInsight * _Nullable)insight;
+ (void)OnExternalMediationRequestWithInterstitial:(MAInterstitialAd * _Nonnull)interstitial;
+ (void)OnExternalMediationRequestWithInterstitial:(MAInterstitialAd * _Nonnull)interstitial customBidPrice:(double)customBidPrice;
+ (void)OnExternalMediationRequestWithRewarded:(MARewardedAd * _Nonnull)rewarded insight:(AdInsight * _Nullable)insight;
+ (void)OnExternalMediationRequestWithRewarded:(MARewardedAd * _Nonnull)rewarded;
+ (void)OnExternalMediationRequestWithRewarded:(MARewardedAd * _Nonnull)rewarded customBidPrice:(double)customBidPrice;
+ (void)OnExternalMediationRequest:(AdType)adType id:(NSString * _Nonnull)id requestedAdUnitId:(NSString * _Nonnull)requestedAdUnitId requestedFloor:(double)requestedFloor requestId:(NSInteger)requestId;

+ (void)OnExternalMediationRequestLoadWithBanner:(MAAdView * _Nonnull)banner ad:(MAAd * _Nonnull)ad;
+ (void)OnExternalMediationRequestLoadWithInterstitial:(MAInterstitialAd * _Nonnull)interstitial ad:(MAAd * _Nonnull)ad;
+ (void)OnExternalMediationRequestLoadWithRewarded:(MARewardedAd * _Nonnull)rewarded ad:(MAAd * _Nonnull)ad;

+ (void)OnExternalMediationRequestFailWithBanner:(MAAdView * _Nonnull)banner error:(MAError * _Nonnull)error;
+ (void)OnExternalMediationRequestFailWithInterstitial:(MAInterstitialAd * _Nonnull)interstitial error:(MAError * _Nonnull)error;
+ (void)OnExternalMediationRequestFailWithRewarded:(MARewardedAd * _Nonnull)rewarded error:(MAError * _Nonnull)error;

+ (void)OnExternalMediationImpression:(MAAd* _Nonnull)ad;
+ (void)OnExternalMediationClick:(MAAd* _Nonnull)ad;

@end

#endif
