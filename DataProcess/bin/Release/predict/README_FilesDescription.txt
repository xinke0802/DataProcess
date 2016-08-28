This file describes the folders and files in this folder. Every important sub-folder will also has a files description README in themselves.

Files Description
==========================
Files in this folder are predict, label and evaluation files of rumor ranking task using feature set selected by different feature selection algorithm and different classifiers (decition tree, naive bayesian or multi-classifiers). 

Every file except "selection_feature.txt" will be named by the rules below:

Prefix
    "predict": Files with this prefix are predict files produced by classifers. The first number every line is the posterior probability of being rumor, and the second number is the tweet cluster ID # predicted.
    
    "text": Files with this prefix are ground truth label files for predicted rumor. Every three lines are for one prediction. The first line are title - for example, "[2] 0.99998, 929: 582476" means it's the 2nd prediction, posterior probability is 0.99998, original tweet cluster ID # is 929, and the representative tweet ID # is 582476. The second line is the text of representative tweet of cluster predicted. And the third line is the ground truth label - rumor (1) and non-rumor (0). Only top 100 predictions are manually labeled.
    
    "evaluation": Files with this prefix are evaluation files of ranking. Every file contains top 20, 50 and 100 precision.
    
Infix
    "_15": Files with this infix use the 15 features in paper "Enquiring Minds Early Detection of Rumors in Social Media from Enquiry Posts"
    
    "_45": Files with this infix use all 45 features in Yangxin's thesis.
    
    "_corr": Files with this infix use the feature set selected by filter feature selection algorithm based on pearson correlation.
    
    "_nmi": Files with this infix use the feature set selected by filter feature selection algorithm based on NMI evaluation.
    
    "_relief": Files with this infix use the feature set selected by filter feature selection algorithm called "relief".
    
    "_forward": Files with this infix use the feature set selected by wrapper feature selection algorithm with forward searching mode.
    
    "_backward": Files with this infix use the feature set selected by wrapper feature selection algorithm with backward searching mode.
    
    "_float": Files with this infix use the feature set selected by wrapper feature selection algorithm with float searching mode and the initial feature set of searching is produced by a certain filter feature selection algorithm.
    
    ## Some files don't have any infix means they use multi-classifiers voting method in ranking, and these classifers use different feature set which are selected by 6 different feature selection algorithm.
    
Suffix
    "_DT": Files with this suffix use decision tree as classifier. (Or 6 decision trees voting)
    
    "_NB": Files with this suffix use naive bayesian classifier. (Or 6 naive bayesian classifiers voting)
    
    "_All": Files with this suffix use 6 decision trees and 6 naive bayesian classiers with different selected feature set to vote for the most likely rumors.
    
    
selection_feature.txt
    The selected feature set used by each file.
