This file describes the folders and files in this folder. Every important sub-folder will also has a files description README in themselves.

Folders Description
==========================
000 & 011 & 100 & jaccard & tfIdf & new_test
    Files in these folders are tentative experiment results. They are obsolete and not reported in thesis.
    
combine
    Files in this folder are tweet secondary clustering experiment results using different clustering methods. Tweets in Nov, 2014 are used in the experiment (939 original clusters and 686 real clusters). Grid search is employed to find the optimum weights of 6 similarities.
    
combine_NoNoise
    Files in this folder are tweet secondary clustering experiment results using different clustering methods. Tweets in Nov, 2014 are used in the experiment and noisy real clusters, of which tweets number are less than 10, are deleted from dataset (214 original clusters and 67 real clusters). Grid search is employed to find the optimum weights of 6 similarities.
    
baseline
    Files in this folder are tweet secondary clustering baseline experiment results using different clustering methods. Tweets in Nov, 2014 are used in the experiment (939 original clusters and 686 real clusters). The similarities used in the baselines are semantic similarities using CM and greedy algorithm from package SEMILAR.
    
baseline_NoNoise
    Files in this folder are tweet secondary clustering experiment results using different clustering methods. Tweets in Nov, 2014 are used in the experiment and noisy real clusters, of which tweets number are less than 10, are deleted from dataset (214 original clusters and 67 real clusters). The similarities used in the baselines are semantic similarities using CM and greedy algorithm from package SEMILAR.

Avg_Weight
    Files in this folder are tweet secondary clustering experiment results using different clustering methods. Tweets in Nov, 2014 are used in the experiment: both noisy version (A) and de-noise version (B). The average weights of multi-experiments are applied on 6 similarities in these experiments. Plus, the experiments to study how the cluster number parameter K has an effect on clustering evaluation NMI are also in this folder. And different modes of hierarchical clustering are employed for clustering in these experiments.


Files Description
==========================
main.m
    This file contains demo/experiment codes for tweet secondary clustering, feature selection, rumor classification and ranking. It shows the usage of other *.m files.
    
sc.m
    Spectral clustering in Yangqiu's paper.
    
nmi.m
    NMI calculator.
    
knGauss.m
    Kernel Gauss function calculator.
    
k_means.m
    K-means implementation. It's from the released codes of Yangqiu's spectral clustering paper.
    
knKmeans.m
    Kernel K-means implementation by Mo Chen.
    
Filter_corr.m
    Implementation of filter feature selection based on pearson/spearman correlation.
    
Filter_nmi.m
    Implementation of filter feature selection based on NMI evaluation.
    
Filter_relief.m
    Implementation of RELIEF filter feature selection.
    
Wrapper.m
    Implementation of wrapper feature selection algorithm.
    
TrainAndTest.m
    Train and test for rumor classification with certain classifier and selected feature set. N-fold evaluation is used.
    
EvaluateSelection.m
    Training and overall testing for a selected feature set in rumor classification task using both decision tree and naive bayesian classifiers under 3 kinds of evaluation: F1 measure, accuracy and precision.
    
EvaluateVote.m
    Traing and overall testing for rumor classification task using multi-classifiers voting with different selected feature set by N-fold cross-validation.
    
Evaluation.m
    Evaluate one selected feature set with 10-fold cross-validation using decision tree and naive bayesian under 3 different evaluations: F1 measure, accuracy and precision. Or overall evaluation for the multi-classifiers voting. (Rumor classification task)
        
PredictAndRank.m
    Predict and rank rumors with selected feature set using decision tree or naive bayesian. (Rumor ranking task)
    
PredictAndVote.m
    Predict rumors with multi-classifiers voting technique. (Rumor ranking task)
    
selection.txt
    Result file of feature selection. It contains the result of 1) filter feature selections, 2) wrapper feature selections with forward and backward searching mode, and 3) wrapper with float searching mode and initial feature set selected by filter.
    
label_clusterRumor.txt
    Ground truth label file to tell whether a real tweet cluster is a real rumor topic (1) or not (0). There are 686 clusters in total.
    
evaluation_10Fold.txt
    The evaluation of rumor classification with decision tree and naive bayes using different selected feature set. The evaluation format is as below (Acc - accuracy; DT - decision tree; NB - naive bayes):
    [meanAcc_DT meanF1_DT meanPrecision_DT stdAcc_DT stdF1_DT stdPrecision_DT;
    meanAcc_NB meanF1_NB meanPrecision_NB stdAcc_NB stdF1_NB stdPrecision_NB]
    
selection_temp.txt & selection_temp1.txt & evaluation_temp.txt & predict.txt
    Temporary files of results of feature selection, 10-fold evaluation and rumor ranking prediction.
    
predict_DT.txt & predict_NB.txt & predict_All.txt
    Result files of rumor ranking task using multi-classifiers voting technique. The 3 files are result of 6 decision trees voting, 6 naive beyes classifiers voting and 6 DT + 6 NB voting.
